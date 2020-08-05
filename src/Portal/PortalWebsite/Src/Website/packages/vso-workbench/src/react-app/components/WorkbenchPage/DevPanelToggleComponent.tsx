import * as React from 'react';
import { devPanelSectionsStorage } from './DevPanelSectionsStateStorage';

export interface IDevPanelToggleComponentProps {
    id: string;
    isOnByDefault?: boolean;
    isAccountForChildEvents?: boolean;
}
export interface IDevPanelToggleComponentState {
    isOn: boolean;
}

export class DevPanelToggleComponent<T, K> extends React.Component<
    T & IDevPanelToggleComponentProps,
    IDevPanelToggleComponentState
> {
    constructor(props: T & IDevPanelToggleComponentProps) {
        super(props);

        const { isOnByDefault, id } = props;

        let lsValue = devPanelSectionsStorage.getItem(id);
        if (isOnByDefault && lsValue == null) {
            lsValue = 'true';
        }

        const isOn = `${lsValue}` === 'true';

        this.onChange(isOn);
        this.state = {
            isOn,
        };
    }

    protected onChange(value: boolean) {}

    protected onToggle = (e: React.BaseSyntheticEvent) => {
        const { id, isAccountForChildEvents } = this.props;
        const { isOn } = this.state;

        if (!isAccountForChildEvents && e.currentTarget !== e.target) {
            return;
        }

        const newIsOn = !isOn;

        devPanelSectionsStorage.setItem(id, newIsOn);

        this.onChange(newIsOn);

        this.setState({
            isOn: newIsOn,
        });
    };
}
