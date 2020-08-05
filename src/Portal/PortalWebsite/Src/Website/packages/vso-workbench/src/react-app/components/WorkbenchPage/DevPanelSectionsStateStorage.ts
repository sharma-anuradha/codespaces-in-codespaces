const LS_KEY = 'vscs-dev-panel-section-states';

type TSerializable = string | number | boolean | object;

class DevPanelSectionsStorage {
    private getData = () => {
        try {
            const dataString = localStorage.getItem(LS_KEY) || '{}';
            const data = JSON.parse(dataString);
            return data;
        } catch {
            return {};
        }
    };

    private saveData = (data: Record<string, TSerializable>) => {
        try {
            localStorage.setItem(LS_KEY, JSON.stringify(data));
        } catch {
            localStorage.setItem(LS_KEY, '{}');
        }
    };

    public getItem = <T>(id: string) => {
        const data = this.getData();

        return data[id] as T | undefined;
    };

    public setItem = (id: string, value: TSerializable) => {
        const data = this.getData();

        data[id] = value;

        this.saveData(data);
        return this;
    };

    public deleteItem = (id: string) => {
        const data = this.getData();
        delete data[id];

        this.saveData(data);
    };
}

export const devPanelSectionsStorage = new DevPanelSectionsStorage();
