public interface ICardTargetValidator
{
    string ValidatorId { get; }

    CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target);
}
