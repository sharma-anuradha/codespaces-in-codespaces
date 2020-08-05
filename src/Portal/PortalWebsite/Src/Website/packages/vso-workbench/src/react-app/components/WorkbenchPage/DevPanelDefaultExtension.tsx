import * as React from 'react';
import { VSCodeExtension } from 'vs-codespaces-authorization';

import { DevPanelToggleComponent } from './DevPanelToggleComponent';
import {
    addRequiredExtensionExclusion,
    removeRequiredExtensionExclusion,
} from '../../../vscode/workbenches/getDefaultExtensions';

interface IDevPanelDefaultExtensionProps {
    id: string;
    extension: VSCodeExtension;
}

export class DevPanelDefaultExtension extends DevPanelToggleComponent<
    IDevPanelDefaultExtensionProps,
    object
> {
    public onChange(value: boolean) {
        const { extension } = this.props;

        if (!value) {
            return addRequiredExtensionExclusion(extension.id);
        }

        removeRequiredExtensionExclusion(extension.id);
    }

    render() {
        const { extension, id } = this.props;

        return (
            <p key={id}>
                <input
                    id={id}
                    className='vscs-dev-panel__input'
                    type='checkbox'
                    checked={this.state.isOn}
                    onChange={this.onToggle}
                />
                <label htmlFor={id}>{extension.id}</label>
            </p>
        );
    }
}
