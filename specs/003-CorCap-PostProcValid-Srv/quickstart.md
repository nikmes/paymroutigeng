# Quickstart — Phase 1.3 Corridor Capabilities and Post-Processing

## Configure capabilities
- Create `config/capabilities.json` (or use the sample) with BIC→currency→nostro mappings.

## Wire the pipeline (pseudocode)
var enriched = await enricher.Enrich(request);
var decision = await engineHost.EvaluateAsync(enriched); // provisional GREEN/RED
var caps = await capabilitiesStore.GetSnapshotAsync();
decision = postProcessor.Process(request, decision, caps);
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
      "nostroIban": "DE12123456789012345678",
      "description": "Send outbound EUR payments to Greece via Deutsche Bank"
    }
  ],
  "redRoutes": []
}

