# Quickstart — Phase 1.3 Corridor Capabilities and Post-Processing

## Configure capabilities
- Create `config/capabilities.json` (or use the sample) with BIC→currency→nostro mappings and `supportedCharges` per currency.

## Wire the pipeline (pseudocode)
var enriched = await enricher.Enrich(request);
var decision = await engineHost.EvaluateAsync(enriched); // provisional GREEN/RED
var caps = await capabilitiesStore.GetSnapshotAsync();
decision = postProcessor.Process(request, decision, caps); // validates currency + charge-bearer; enriches nostroIban
return decision;

## Response example (GREEN annotated with nostroIban)
{
  "status": "EVALUATED",
  "decision": "CAN_ROUTE",
  "greenRoutes": [
    {
      "ruleCode": "RULE-OUT-GR-EUR",
      "corrBankBic": "DEUTDEFFXXX",
      "currency": "EUR",
      "chargeBearer": "SHA",
      "nostroIban": "DE12123456789012345678",
      "description": "Send outbound EUR payments to Greece via Deutsche Bank"
    }
  ],
  "redRoutes": []
}

## Discovery: list supported currencies before user selects one

You can pre-limit the currency picker in UI without a selected currency yet. Two pragmatic options:

1) Capabilities-only (fast, simplest)
   - If you know the target correspondent BIC, list its supported currencies and charge-bearers straight from the capabilities snapshot.

   Pseudocode:

   ```csharp
   var caps = await capabilitiesStore.GetSnapshotAsync();
   if (caps.BicToCurrencyCapabilities.TryGetValue(corrBankBic, out var byCurrency))
   {
     var supportedCurrencyCodes = byCurrency.Keys.OrderBy(x => x).ToList();
     // optionally include supportedCharges per currency
     var dto = byCurrency.Select(kvp => new {
       currency = kvp.Key,
       nostroIban = kvp.Value.NostroIban,
       supportedCharges = kvp.Value.SupportedCharges?.OrderBy(x => x).ToArray() ?? Array.Empty<string>()
     });
   }
   ```

2) Rules-aware discovery (accurate to current rules)
   - If you only have minimal inputs (e.g., direction, counterparty country/BIC, customerId) and want the currencies that would actually produce a route, probe the engine per currency derived from capabilities and collect those that return GREEN.

   Pseudocode:

   ```csharp
   var caps = await capabilitiesStore.GetSnapshotAsync();
   var allCurrencies = caps.BicToCurrencyCapabilities
     .SelectMany(kvp => kvp.Value.Keys)
     .ToHashSet(StringComparer.OrdinalIgnoreCase);

   var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach (var cur in allCurrencies)
   {
     var req = baseRequest with { Payment = baseRequest.Payment with { Currency = cur, ChargeBearer = null } };
     var result = await engineHost.EvaluateAsync(req);
     if (result.Decision == RoutingDecision.CanRoute && result.GreenRoutes.Count > 0)
     {
       supported.Add(cur);
     }
   }
   // supported now holds the currencies that your current rules + capabilities accept for the given corridor
   ```

Notes:
- If you already know (or prefer) a charge-bearer, add it to the probe to filter out currencies where the corridor doesn’t support that charge-bearer.
- The rules-aware approach scales fine for dozens of currencies; you can cache results per corridor (e.g., by counterparty bank/country) and refresh on capability/rule snapshot changes.

## Discovery: supported countries without BIC/country

When you don’t yet know the counterparty BIC or country, you can still suggest destination countries supported by your current rules and capabilities. Two approaches:

1) Rules introspection (fast):
   - Scan the rule catalog for conditions referencing `Counterparty.BankCountryCode` and collect the distinct country codes for the current product/direction/customer segment.
   - Pros: No engine runs. Cons: If a rule omits country (wildcard), you can’t infer precise coverage; also ignores capability gaps.

2) Rules-aware probe (precise):
   - Start from a candidate country list (e.g., ISO 3166 whitelist for your business). For each country C:
   - Derive candidate currencies from the capabilities union (or a configured subset for that market).
   - For each currency cur, evaluate with a minimal request: direction + currency = cur, counterparty.country = C, and no BIC.
   - If any evaluation returns GREEN, mark country C as supported.

   Pseudocode:

   ```csharp
   var caps = await capabilitiesStore.GetSnapshotAsync();
   var allCurrencies = caps.BicToCurrencyCapabilities
     .SelectMany(kvp => kvp.Value.Keys)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

   var supportedCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach (var country in candidateCountries)
   {
     var any = false;
     foreach (var cur in allCurrencies)
     {
       var req = baseRequest with
       {
         Payment = baseRequest.Payment with { Currency = cur, ChargeBearer = preferredChargeBearer },
         Counterparty = baseRequest.Counterparty with { BankCountryCode = country, BankBic = null }
       };
       var res = await engineHost.EvaluateAsync(req);
       if (res.Decision == RoutingDecision.CanRoute && res.GreenRoutes.Count > 0)
       {
         supportedCountries.Add(country);
         any = true;
         break; // country is supported by at least one currency
       }
     }
     // optional: cache per (direction, segment) for speed; invalidate on snapshot version changes
   }
   ```

Edge cases and tips:
- If your rules require a BIC to match, probing without BIC may not yield GREEN. Prefer modeling selection by country/attributes in rules, leaving BIC as an outcome; or add dedicated "discovery" rules that intentionally match on country.
- Combine with capabilities to avoid probing currencies a corridor cannot support anyway.
- Cache discovery results keyed by (direction, segment) and the snapshots’ versions for snappy UI.

## Single-call eligibility (CustomerId only)

Goal: With only `customerId` (and optional preferred `direction`), return everything the UI needs to render valid choices:
- Available directions (IN, OUT, INT, OWN) that can produce a GREEN route for the customer
- Available currencies for those directions
- Available destination countries for those directions
- Supported charge-bearers (BEN/SHA/OWN) observed across any GREEN route combinations

Approach
1) Build candidate sets:
   - Directions: from your product policy (e.g., {"IN","OUT","INT","OWN"})
   - Currencies: union of currencies from capabilities snapshot
   - Countries: either from a configured whitelist or by introspecting rules predicates for `PR.CPartyBankCountryCode`
2) For each direction D:
   - For each country C and currency cur, evaluate with minimal request (no BIC). Run full pipeline (evaluation + post-processing) so capability constraints (currency/charge-bearer) apply.
   - If GREEN exists, record D as supported; add C to D.countries; add cur to D.currencies.
   - For each GREEN route, look up capabilities for (route.CorrBankBic, cur) and collect `supportedCharges` into D.chargeBearersAny (union) and D.chargeBearersCommon (intersection).
3) Return a compact DTO with per-direction aggregates and snapshot versions for caching.

Pseudocode

```csharp
public sealed record EligibilityRequest(
  string CustomerId,
  string? PreferredDirection = null,
  string[]? CurrencyWhitelist = null,
  string[]? CountryWhitelist = null,
  string? PreferredChargeBearer = null // BEN|SHA|OWN
);

public sealed record DirectionEligibility(
  string Direction,
  string[] Countries,
  string[] Currencies,
  string[] ChargeBearersAny,
  string[] ChargeBearersCommon
);

public sealed record EligibilityResponse(
  long RulesVersion,
  long CapabilitiesVersion,
  DirectionEligibility[] Directions
);

public async Task<EligibilityResponse> ComputeEligibilityAsync(EligibilityRequest req)
{
  var caps = await capabilitiesStore.GetSnapshotAsync();
  var rules = await ruleStore.GetSnapshotAsync();

  var allCurrencies = (req.CurrencyWhitelist?.ToHashSet(StringComparer.OrdinalIgnoreCase)
    ?? caps.BicToCurrencyCapabilities.SelectMany(kvp => kvp.Value.Keys)
        .ToHashSet(StringComparer.OrdinalIgnoreCase));

  var candidateCountries = (req.CountryWhitelist?.ToHashSet(StringComparer.OrdinalIgnoreCase)
    ?? LoadCandidateCountriesFromRules(rules)); // or a configured whitelist

  var directions = req.PreferredDirection != null
    ? new[] { req.PreferredDirection.ToUpperInvariant() }
    : new[] { "IN", "OUT", "INT", "OWN" };

  var results = new List<DirectionEligibility>();
  foreach (var dir in directions)
  {
    var countries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    HashSet<string>? commonCharges = null; // intersection accumulator
    var anyCharges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var country in candidateCountries)
    {
      foreach (var cur in allCurrencies)
      {
        var baseCtx = new RoutingContext(
          new PaymentContext(ParseDirection(dir), cur, req.PreferredChargeBearer),
          new CounterpartyContext(country, bankBic: null, account: null, name: null, type: null),
          new CustomerContext(req.CustomerId, industry: null, type: null, account: null));

        var decision = await engineHost.EvaluateAsync(baseCtx);
        if (decision.Decision == RoutingDecision.CanRoute && decision.GreenRoutes.Count > 0)
        {
          countries.Add(country);
          currencies.Add(cur);

          // Collect charge-bearers from capabilities for each GREEN route
          if (caps.BicToCurrencyCapabilities is { } map)
          {
            foreach (var route in decision.GreenRoutes)
            {
              if (map.TryGetValue(route.CorrBankBic, out var byCur) && byCur.TryGetValue(cur, out var cc))
              {
                var supported = (cc.SupportedCharges ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (commonCharges is null)
                {
                  commonCharges = new HashSet<string>(supported, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                  commonCharges.IntersectWith(supported);
                }
                anyCharges.UnionWith(supported);
              }
            }
          }
        }
      }
    }

    if (countries.Count > 0)
    {
      results.Add(new DirectionEligibility(
        dir,
        countries.OrderBy(x => x).ToArray(),
        currencies.OrderBy(x => x).ToArray(),
        anyCharges.OrderBy(x => x).ToArray(),
        (commonCharges ?? new HashSet<string>()).OrderBy(x => x).ToArray()));
    }
  }

  return new EligibilityResponse(rules.Version, caps.Version, results.ToArray());
}
```

Response example

```json
{
  "rulesVersion": 12,
  "capabilitiesVersion": 5,
  "directions": [
  {
    "direction": "OUT",
    "countries": ["GR", "DE", "CY"],
    "currencies": ["EUR", "USD"],
    "chargeBearersAny": ["OWN", "SHA"],
    "chargeBearersCommon": ["SHA"]
  },
  {
    "direction": "IN",
    "countries": ["GR"],
    "currencies": ["EUR"],
    "chargeBearersAny": ["BEN", "SHA"],
    "chargeBearersCommon": ["SHA"]
  }
  ]
}
```

Notes
- Charge-bearers: allowed values are [BEN, SHA, OWN]. If legacy inputs contain OUR, normalize to OWN; present OWN in responses.
- UI can show only options found in `chargeBearersAny` to avoid dead ends; for stricter UX, use `chargeBearersCommon` until country/currency is chosen.
- Cache the response per (customerId, preferredDirection) and invalidate when `rulesVersion` or `capabilitiesVersion` changes.

