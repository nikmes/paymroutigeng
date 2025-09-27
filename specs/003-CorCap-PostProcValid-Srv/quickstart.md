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

