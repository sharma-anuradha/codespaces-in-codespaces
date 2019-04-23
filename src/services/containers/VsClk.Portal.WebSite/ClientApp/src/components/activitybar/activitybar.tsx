import React, { Component } from 'react';
import './activitybar.css';

export interface ActivityBarProps {
    position: 'left';
}

export class ActivityBar extends Component<ActivityBarProps> {
    render() {
        const { position } = this.props;

        return (
            <div className={`part activitybar ${position}`}
                style={{
                    backgroundColor: 'rgb(51, 51, 51)',
                    top: '30px', bottom: '0px', left: '0px',
                    position: 'absolute', height: 'calc(100% - 30px)'
                }}>
                <div className='content'>
                    <div className='composite-bar'>
                        <div className='monaco-action-bar vertical'>
                            <ul className='actions-container' role='toolbar' aria-label='Active View Switcher'>
                                <li className='action-item disabled' role='button' draggable={true} tabIndex={0} aria-label='Explorer (⇧⌘E) active' title='Explorer (⇧⌘E)'>
                                    <a className='action-label explore' aria-label='Explorer (⇧⌘E)' title='Explorer (⇧⌘E)' style={{ backgroundColor: 'rgba(255, 255, 255, 0.6)' }}></a>
                                </li>
                                <li className='action-item disabled' role='button' draggable={true} tabIndex={0} aria-label='Search (⇧⌘F)' title='Search (⇧⌘F)'>
                                    <a className='action-label search' aria-label='Search (⇧⌘F)' title='Search (⇧⌘F)' style={{ backgroundColor: 'rgba(255, 255, 255, 0.6)' }}></a>
                                </li>
                                <li className='action-item disabled' role='button' draggable={true} tabIndex={0} aria-label='Source Control (⌃⇧G)' title='Source Control (⌃⇧G)'>
                                    <a className='action-label scm' aria-label='Source Control (⌃⇧G)' title='Source Control (⌃⇧G)' style={{ backgroundColor: 'rgba(255, 255, 255, 0.6)' }}></a>
                                </li>
                            </ul>
                        </div>
                    </div>
                </div>
            </ div >
        );
    }
}
