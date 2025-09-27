namespace RoutingEngine.Domain;

/// <summary>
/// Captures per-rule evaluation diagnostics for auditing and troubleshooting.
/// </summary>
/// <param name="RuleCode">Rule identifier evaluated.</param>
/// <param name="Matched">True when the rule conditions were satisfied.</param>
/// <param name="OutcomePolicy">Policy associated with the rule.</param>
/// <param name="PriorityWeight">Priority weight applied during ordering.</param>
/// <param name="CorrBankBic">Correspondent corridor associated with the rule.</param>
/// <param name="EvaluationDuration">Time spent evaluating the rule.</param>
public sealed record RuleEvaluationAuditRecord(
	string RuleCode,
	bool Matched,
	OutcomePolicy OutcomePolicy,
	int PriorityWeight,
	string CorrBankBic,
	TimeSpan EvaluationDuration);
