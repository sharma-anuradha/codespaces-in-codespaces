import * as React from 'react';
import { VSCodeExtension } from 'vs-codespaces-authorization';

import { DevPanelSection } from './DevPanelSection';
import { DevPanelDefaultExtension } from './DevPanelDefaultExtension';

interface IDevPanelDefaultExtensionsSectionProps {
    defaultExtensions: VSCodeExtension[];
}

interface IDevPanelDefaultExtensionsSectionState {
    excludedExtensions: VSCodeExtension[];
}

export class DevPanelDefaultExtensionsSection extends React.Component<
    IDevPanelDefaultExtensionsSectionProps,
    IDevPanelDefaultExtensionsSectionState
> {
    constructor(props: IDevPanelDefaultExtensionsSectionProps) {
        super(props);

        this.state = {
            excludedExtensions: [],
        };
    }

    public render() {
        const { defaultExtensions } = this.props;

        const extensions: React.ReactNode[] = defaultExtensions.map((extension) => {
            const id = `vscs-dev-panel-default-extension-${extension.id}`;

            return (
                <DevPanelDefaultExtension
                    key={id}
                    id={id}
                    extension={extension}
                    isOnByDefault={true}
                />
            );
        });

        return (
            <DevPanelSection
                id='dev-panel-default-extensions-section'
                title='Required Extensions'
                isOnByDefault={true}
            >
                {extensions}
            </DevPanelSection>
        );
    }
}
