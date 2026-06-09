using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Baseline validator used by the AI legal-action reader until full validator routing is available.
    /// </summary>
    public sealed class BasicComputerTargetValidator : ICardTargetValidator
    {
        private static readonly FallbackTargetValidator FallbackValidator = new FallbackTargetValidator();

        public string ValidatorId => "computer.basic";

        public CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null || sourceCard?.SourceCard == null)
            {
                return FallbackValidator.Validate(context, sourceCard, target);
            }

            ReusableTargetRulesValidator reusableValidator = Object.FindFirstObjectByType<ReusableTargetRulesValidator>();
            if (reusableValidator != null
                && string.Equals(sourceCard.SourceCard.validatorId, reusableValidator.ValidatorId, System.StringComparison.OrdinalIgnoreCase))
            {
                return reusableValidator.Validate(context, sourceCard, target);
            }

            return FallbackValidator.Validate(context, sourceCard, target);
        }
    }
}
