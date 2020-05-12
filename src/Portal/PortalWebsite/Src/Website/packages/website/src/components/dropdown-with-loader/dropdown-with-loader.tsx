import React, { Component } from 'react';
import { Dropdown, IDropdownProps, IDropdown } from 'office-ui-fabric-react/lib/Dropdown';

import { Loader } from '../loader/loader';

import './dropdown-with-loader.css';

interface DropDownWithLoaderProps extends IDropdownProps {
    loadingMessage: string;
    isLoading: boolean;
    shouldFocus?: boolean;
}

const loaderOptionKey = 'loader';

export class DropDownWithLoader extends Component<DropDownWithLoaderProps> {
    private dropdownRef = React.createRef<IDropdown>();

    componentDidMount() {
        const { shouldFocus } = this.props;
        if (shouldFocus && this.dropdownRef.current) {
            this.dropdownRef.current.focus();
        }
    }

    render() {
        const { className = '' } = this.props;

        return (
            <div className={`vsonline-dropdown-with-loader ${className}`}>
                {this.getPayload()}
            </div>
        );
    }

    private getPayload() {
        let {
            isLoading,
            loadingMessage,
            options,
        } = this.props;

        const optionsToRender = (isLoading)
            ? [{ key: loaderOptionKey, text: ''}]
            : options;

        const selectedKey = (isLoading)
            ? loaderOptionKey
            : this.props.selectedKey;

        const disabled = (isLoading)
            ? true
            : this.props.disabled;

        const onRenderTitle = (isLoading)
            ? (() => {
                return (
                    <Loader
                        message={loadingMessage}
                        className='vsonline-dropdown-with-loader__loader' />
                );
            })
            : void 0;

        return (<Dropdown
                {...this.props}
                options={optionsToRender}
                selectedKey={selectedKey}
                disabled={disabled}
                onRenderTitle={onRenderTitle}
                componentRef={this.dropdownRef}
            />);
    }
}
