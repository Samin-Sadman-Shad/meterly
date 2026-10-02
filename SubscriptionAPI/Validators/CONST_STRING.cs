namespace SubscriptionAPI.Validators
{
    public static class CONST_STRING
    {
        public const string IsRequired = "{PropertyName} is required.";
        public const string InvalidEmailAddress = "{PropertyName} must be a valid email address.";
        public const string MustBeValidTimestamp = "{PropertyName} must be a valid timestamp.";
        public const string MustBeLaterThan = "{PropertyName} must be later than {ComparisonValue}.";
        public const string MustReferencePersistedPlan = "{PropertyName} must reference a persisted plan.";
        public const string MustReferencePersistedPlanWhenSupplied = "{PropertyName} must reference a persisted plan when supplied.";
        public const string DoesNotExist = "The selected '{PropertyName}' does not exist.";
        public const string MustDifferFromPreviousPlan = "{PropertyName} must differ from 'Previous Plan'.";
        public const string PlanAlreadyExists = "A plan with the given 'Title' and 'Version' already exists.";
        public const string MaxLengthExceeded = "{PropertyName} must be no more than {MaxLength} characters.";
    }
}
