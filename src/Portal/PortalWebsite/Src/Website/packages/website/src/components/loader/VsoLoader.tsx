import React, { Component } from 'react';
import { Spinner } from 'office-ui-fabric-react/lib/Spinner';
import { LoaderProps } from './loader';

export class VsoLoader extends Component<LoaderProps> {
    render() {
        const { 
            message = this.props.translation('loading'),
            labelPosition = 'right',
            className = '',
        } = this.props;
        return (<Spinner
                    className={`vsonline-loader ${className}`}
                    label={message}
                    ariaLabel={message}
                    ariaLive='assertive'
                    labelPosition={labelPosition}
                />);
    }
}
