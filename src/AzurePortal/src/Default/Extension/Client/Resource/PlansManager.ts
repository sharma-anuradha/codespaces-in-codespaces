import { PlanResource } from "./PlanResource";

export interface PlansManager {
    fetchPlan(id?: string): Q.Promise<PlanResource>;
}