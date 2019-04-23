import React, { Component } from 'react';
import './titlebar.css';

export interface TitleBarProps {
}

export class TitleBar extends Component<TitleBarProps> {
    render() {

        return (
            <div className='titlebar part' style={{ backgroundColor: 'rgb(60, 60, 60', color: 'rgb(204, 204, 204)', height: '30px' }}>
                <div className='window-appicon' style={{ 'WebkitAppRegion': 'drag' } as any}></div>
                <div className='menubar' role='menubar' style={{ height: '30px' }}>
                    <div className='menubar-menu-button' role='menuitem' tabIndex={-1} aria-label='File' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>File</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='Edit' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>Edit</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='Selection' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>Selection</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='View' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>View</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='Go' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>Go</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='Debug' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>Debug</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='Terminal' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>Terminal</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='Help' aria-haspopup='true'>
                        <div className='menubar-menu-title' role='none' aria-hidden='true'>Help</div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='...' aria-haspopup='true' style={{ visibility: 'hidden' }}>
                        <div className='menubar-menu-title toolbar-toggle-more' role='none' aria-hidden='true'></div>
                    </div>
                    <div className='menubar-menu-button disabled' role='menuitem' tabIndex={-1} aria-label='...' aria-haspopup='true' style={{ visibility: 'hidden' }}>
                        <div className='menubar-menu-title toolbar-toggle-more' role='none' aria-hidden='true'></div>
                    </div>
                </div>
                <div className='window-title' style={{ position: 'absolute', left: '50%', transform: 'translate(-50%, 0px)' }}>Welcome</div>
            </ div>
        );
    }
}
