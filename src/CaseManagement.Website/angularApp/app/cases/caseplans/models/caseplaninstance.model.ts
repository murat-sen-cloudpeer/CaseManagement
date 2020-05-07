export class StateHistory {
    State: string;
    DateTime: string;

    public static fromJson(json: any): StateHistory{
        let result = new StateHistory();
        result.State = json["state"];
        result.DateTime = json["datetime"];
        return result;
    }
}

export class TransitionHistory {
    Transition: string;
    DateTime: string;

    public static fromJson(json: any): TransitionHistory {
        let result = new TransitionHistory();
        result.Transition = json["transition"];
        result.DateTime = json["datetime"];
        return result;
    }
}

export class ExecutionHistory {
    StartDateTime: Date;
    EndDateTime: Date;
    Id: string;

    public static fromJson(json: any): ExecutionHistory {
        let result = new ExecutionHistory();
        result.StartDateTime = json["start_datetime"];
        result.EndDateTime = json["end_datetime"];
        result.Id = json["id"];
        return result;
    }
}

export class CasePlanElementInstance {
    constructor() {
        this.StateHistories = [];
        this.TransitionHistories = [];
    }

    Id: string;
    Version: string;
    CreateDateTime: Date;
    DefinitionId: string;
    FormInstanceId: string;
    State: string;
    Type: string;
    StateHistories: StateHistory[];
    TransitionHistories: TransitionHistory[];


    public static fromJson(json: any): CasePlanElementInstance {
        let result = new CasePlanElementInstance();
        result.Id = json["id"];
        result.Version = json["version"];
        result.CreateDateTime = json["create_datetime"];
        result.DefinitionId = json["definition_id"];
        result.FormInstanceId = json["form_instanceid"];
        result.State = json["state"];
        result.Type = json["type"];
        json["state_histories"].forEach(function (sh: any) {
            result.StateHistories.push(StateHistory.fromJson(sh));
        });
        json["transition_histories"].forEach(function (th: any) {
            result.TransitionHistories.push(TransitionHistory.fromJson(th));
        });

        return result;
    }
}

export class CasePlanInstance {
    constructor() {
        this.StateHistories = [];
        this.TransitionHistories = [];
        this.ExecutionHistories = [];
        this.Elements = [];
    }

    Id: string;
    CreateDateTime: Date;
    DefinitionId: string;
    Context: any;
    State: string;
    StateHistories: StateHistory[];
    TransitionHistories: TransitionHistory[];
    ExecutionHistories: ExecutionHistory[];
    Elements: CasePlanElementInstance[];

    public static fromJson(json: any): CasePlanInstance {
        var result = new CasePlanInstance();
        result.Id = json["id"];
        result.CreateDateTime = json["create_datetime"];
        result.DefinitionId = json["definition_id"];
        result.Context = json["context"];
        result.State = json["state"];
        json["state_histories"].forEach(function (sh: any) {
            result.StateHistories.push(StateHistory.fromJson(sh));
        });
        json["transition_histories"].forEach(function (th: any) {
            result.TransitionHistories.push(TransitionHistory.fromJson(th));
        });
        json["execution_histories"].forEach(function (eh: any) {
            result.ExecutionHistories.push(ExecutionHistory.fromJson(eh));
        });
        json["elements"].forEach(function (elt: any) {
            result.Elements.push(CasePlanElementInstance.fromJson(elt));
        });
        return result;
    }
}