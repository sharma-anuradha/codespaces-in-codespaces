import * as React from 'react';

import classnames from 'classnames';

import {
    IDevPanelToggleComponentProps,
    IDevPanelToggleComponentState,
    DevPanelToggleComponent,
} from './DevPanelToggleComponent';

import './DevPanelSection.css';

interface IDevPanelSectionProps extends IDevPanelToggleComponentProps {
    title: string;
}

interface IDevPanelSectionState extends IDevPanelToggleComponentState {}

type TProps = React.PropsWithChildren<IDevPanelSectionProps>;

export class DevPanelSection extends DevPanelToggleComponent<TProps, IDevPanelSectionState> {
    public render() {
        const { title, children } = this.props;
        const { isOn } = this.state;

        const className = classnames('vscs-dev-panel-section', { 'is-open': isOn });

        return (
            <div className={className}>
                <div className='vscs-dev-panel-section__title' onClick={this.onToggle}>
                    {title}
                </div>
                <div className='vscs-dev-panel-section__body'>{children}</div>
            </div>
        );
    }
}
