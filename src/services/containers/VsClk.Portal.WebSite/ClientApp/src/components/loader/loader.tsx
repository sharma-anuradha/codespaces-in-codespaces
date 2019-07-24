import React, { Component } from 'react';
import './loader.css';
import { Spinner, SpinnerLabelPosition } from 'office-ui-fabric-react/lib/Spinner';

// import background from './background.svg';
// import topCode from './top-code.svg';
// import middleCode from './middle-code.svg';
// import bottomCode from './bottom-code.svg';

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
            className = ''
        } = this.props;

        return (
            <Spinner
                className={`vssass-loader ${className}`}
                label={message}
                ariaLive="assertive"
                labelPosition={labelPosition} />
        );
    }
}
