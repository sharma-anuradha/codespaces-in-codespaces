import React from 'react';
import { PropertiesTelemetryEvent } from '../../../telemetry/sendTelemetry';

export const StepsPane: React.FunctionComponent<{}> = (props) => {
    return (
        <div className="vscs-splash-screen-steps-pane">
            {props.children}
        </div>
    );
};
