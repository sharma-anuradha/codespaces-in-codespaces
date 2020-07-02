import { PlansManager } from "./PlansManager";
import { PlanResource } from "./PlanResource";
import { getArmUri } from "Shared/Endpoints";
import { batch } from "Fx/Ajax";

export class HttpPlansManager implements PlansManager {
    
    fetchPlan(id?: string): Q.Promise<PlanResource> {
        return batch<PlanResource>({
            uri: getArmUri(id),
            setTelemetryHeader: "GetCodespacesPlanResource",
        }).then((batchResponseItem) => {
            return batchResponseItem.content;
        });
    }

}