import React, { Component } from 'react';
import './loader.css';
import { Spinner, SpinnerLabelPosition } from 'office-ui-fabric-react/lib/Spinner';

interface LoaderProps {
    message?: string;
    labelPosition?: SpinnerLabelPosition;
    className?: string;
}

export class Loader extends Component<LoaderProps> {
    render() {
        const {
            message = 'Loading...',
            labelPosition  = 'right',
            className = '',
        } = this.props;

        return (
            <Spinner
                className={`vsonline-loader ${className}`}
                label={message}
                ariaLabel={message}
                ariaLive='assertive'
                labelPosition={labelPosition}
            />
        );
    }
}
