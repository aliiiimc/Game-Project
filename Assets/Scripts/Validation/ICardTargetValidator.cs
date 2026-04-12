// Interface implemented by all target validation strategies.
public interface ICardTargetValidator
{
    // Stable id used for binding validator rules from data assets.
    string ValidatorId { get; }

    // Validates whether a target can be used by a specific card/action.
    CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target);
}
