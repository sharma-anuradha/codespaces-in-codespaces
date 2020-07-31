import * as signalR from "@microsoft/signalr";

export default class Hub {
    private connection: signalR.HubConnection;
    private jsonHubCallbacks: any[] = [];
    private ngrokHubCallbacks: any[] = [];
    private processHubCallbacks: any[] = [];

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hub")
            .withAutomaticReconnect()
            .build();

        if (this.connection.state === signalR.HubConnectionState.Disconnected) {
            // TODO: Add error handling popover to expose errors.
            this.connection.start().catch(err => console.error(err));
        }

        this.connection.on(`newJsonLogBundle`, (response) => {
            try {
                response.forEach(
                    (jsonLog) => {
                        const log = JSON.parse(jsonLog);
                        this.jsonHubCallbacks.map(n => n(log));
                    }
                );
            } catch (error) {
                // TODO: Add error handling popover to expose errors.
                console.log(error);
            }
        });

        this.connection.on(`newJsonLogEntry`, (response) => {
            try {
                const jsonLog = JSON.parse(response);
                this.jsonHubCallbacks.map(n => n(jsonLog));
            } catch (error) {
                // TODO: Add error handling popover to expose errors.
                console.log(error);
            }
        });

        this.connection.on(`newNgrokEvent`, (response) => {
            try {
                this.ngrokHubCallbacks.map(n => n(response));
            } catch (error) {
                // TODO: Add error handling popover to expose errors.
                console.log(error);
            }
        });

        this.connection.on(`processWorkerEvent`, (response) => {
            try {
                this.processHubCallbacks.map(n => n(response));
            } catch (error) {
                // TODO: Add error handling popover to expose errors.
                console.log(error);
            }
        });
    }

    AddProcessHubCallback(callback: any) {
        this.processHubCallbacks.push(callback);
    }

    RemoveProcessHubCallback(callback: any) {
        this.processHubCallbacks.splice(this.processHubCallbacks.indexOf(callback), 1);
    }

    AddNgrokHubCallback(callback: any) {
        this.ngrokHubCallbacks.push(callback);
    }

    RemoveNgrokHubCallback(callback: any) {
        this.ngrokHubCallbacks.splice(this.ngrokHubCallbacks.indexOf(callback), 1);
    }

    AddJsonHubCallback(callback: any) {
        if (!this.jsonHubCallbacks.includes(callback)) {
            this.jsonHubCallbacks.push(callback);
        }
    }

    RemoveJsonHubCallback(callback: any) {
        this.jsonHubCallbacks.splice(this.jsonHubCallbacks.indexOf(callback), 1);
    }

    SendReloadLogMessage() {
        this.connection.invoke("reloadLogs").catch(e => console.error(e));
    }
}