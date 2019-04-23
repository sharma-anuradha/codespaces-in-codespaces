import React, { Component } from 'react';
import './loader.css';

import background from './background.svg';
import topCode from './top-code.svg';
import middleCode from './middle-code.svg';
import bottomCode from './bottom-code.svg';

interface LoaderProps {
    mainMessage: string;
}

export class Loader extends Component<LoaderProps> {
    render() {
        const { mainMessage } = this.props;
        return (
            <div className='vssass-loader'>
                <img className='image' src={background} alt='Background'/>
                <div>
                    <div className='code-animation'>
                        <img className='top-code' src={topCode} alt='Top line of code'/>
                        <img className='middle-code' src={middleCode} alt='Middle lines of code'/>
                        <img className='bottom-code' src={bottomCode} alt='Bottom line of code'/>
                    </div>
                    <div className='vssass-loader-title'>
                        {mainMessage}
                    </div>
                </div>
            </div>
        );
    }
}
