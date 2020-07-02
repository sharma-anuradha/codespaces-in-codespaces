export interface PlanResource {
    readonly id: string;
    readonly name: string;
    readonly type: string;
    readonly location: string;
    readonly properties?: ReadonlyStringMap<any>;
}