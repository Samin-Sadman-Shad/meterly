*Functional requirements*
-•	 users can create, read, and update plans — each plan defines pricing rules and what a subscriber is entitled to.
-•	The system must never mutate an existing plan version — any change to a plan produces a new immutable version, so historical billing always reflects the plan terms active at the time.
-•	Customers can be subscribed to a plan, and that subscription is always tied to a specific plan version.
-•	Customers can upgrade or downgrade their subscription; the system must determine which plan version applies before and after the change, for correct billing.
-•	Customers can cancel or reactivate a subscription.
-•	Publish domain events on plan and subscription state changes (e.g. PlanVersionPublished, SubscriptionCreated, SubscriptionUpgraded, SubscriptionCancelled)