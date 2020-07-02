/* *****************************************************************************
Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at http://www.apache.org/licenses/LICENSE-2.0

THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
MERCHANTABLITY OR NON-INFRINGEMENT.

See the Apache Version 2.0 License for specific language governing permissions
and limitations under the License.
***************************************************************************** */

// Typing for the jQuery library

/*
    Interface for the AJAX setting that will configure the AJAX request
*/
interface JQueryAjaxSettings<T> {
    accepts?: any;
    async?: boolean;
    beforeSend?(jqXHR: JQueryXHR<T>, settings: JQueryAjaxSettings<T>): boolean;
    cache?: boolean;
    complete?(jqXHR: JQueryXHR<T>, textStatus: string): any;
    contents?: { [key: string]: any; };
    // JQuery in the code compares contentType with a boolean value false
    // to check, whether to add default "Content-Type" or not.
    // Correct use:
    // contentType: "text/plain"
    // contentType: false
    contentType?: any;
    context?: any;
    converters?: { [key: string]: any; };
    crossDomain?: boolean;
    data?: any;
    dataFilter?(data: any, ty: any): any;
    dataType?: string;
    error?(jqXHR: JQueryXHR<T>, textStatus: string, errorThrow: string): any;
    global?: boolean;
    headers?: { [key: string]: any; };
    ifModified?: boolean;
    isLocal?: boolean;
    jsonp?: string;
    jsonpCallback?: any;
    mimeType?: string;
    password?: string;
    processData?: boolean;
    scriptCharset?: string;
    statusCode?: { [key: string]: any; };
    success?(data: any, textStatus: string, jqXHR: JQueryXHR<T>): void;
    timeout?: number;
    traditional?: boolean;
    type?: string;
    url?: string;
    username?: string;
    xhr?: any;
    xhrFields?: { [key: string]: any; };
}

interface JQueryPromiseXHRDoneCallback<T> {
    (data: T, textStatus: string, jqXHR: JQueryXHR<T>): void;
}

interface JQueryPromiseXHRFailCallback<T> {
    (jqXHR: JQueryXHR<T>, textStatus: string, errorThrown: any): void;
}

/*
    Interface for the jqXHR object
*/
interface JQueryXHR<T> extends XMLHttpRequest {
    always(...alwaysCallbacks: Array<{ (): void; }>): JQueryXHR<T>;
    done(...doneCallbacks: Array<JQueryPromiseXHRDoneCallback<T>>): JQueryXHR<T>;
    fail(...failCallbacks: Array<JQueryPromiseXHRFailCallback<T>>): JQueryXHR<T>;
    progress(...progressCallbacks: Array<{ (): void; }>): JQueryXHR<T>;
    state(): string;
    promise(target?: any): JQueryXHR<T>;
    then(
        doneCallbacks: JQueryPromiseXHRDoneCallback<T>,
        failCallbacks?: JQueryPromiseXHRFailCallback<T>,
        progressCallbacks?: { (): void; }): JQueryPromise;

    then<UValue>(
        doneCallbacks: { (data: T, textStatus: string, jqXHR: JQueryXHR<T>): UValue },
        failCallbacks?: JQueryPromiseXHRFailCallback<T>,
        progressCallbacks?: { (): void; }): JQueryPromiseV<UValue>;

    then<UValue, UReject>(
        doneCallbacks: { (data: T, textStatus: string, jqXHR: JQueryXHR<T>): UValue },
        failCallbacks?: { (data: T, textStatus: string, jqXHR: JQueryXHR<T>): UReject },
        progressCallbacks?: { (): void; }): JQueryPromiseVR<UValue, UReject>;

    then<UReject>(
        doneCallbacks: JQueryPromiseXHRDoneCallback<T>,
        failCallbacks?: { (data: T, textStatus: string, jqXHR: JQueryXHR<T>): UReject },
        progressCallbacks?: { (): void; }): JQueryPromiseR<UReject>;

    catch(catchCallback: (reason?: any) => any): JQueryXHR<T>;
    finally(finallyCallback: () => any): JQueryXHR<T>;

    overrideMimeType(mimeType: string): void;
    abort(statusText?: string): void;
    responseJSON?: T;
}

/*
    Interface for the JQuery callback
*/
interface JQueryCallback {
    add(...callbacks: Array<{ (): void }>): JQueryCallback;
    add(callbacks: Array<{ (): void }>): JQueryCallback;
    disable(): JQueryCallback;
    disabled(): boolean;
    empty(): JQueryCallback;
    fire(): JQueryCallback;
    fired(): boolean;
    fireWith(context: any): JQueryCallback;
    has(callback: { (): void }): boolean;
    lock(): JQueryCallback;
    locked(): boolean;
    remove(...callbacks: Array<{ (): void }>): JQueryCallback;
    remove(callbacks: Array<{ (): void }>): JQueryCallback;
}

interface JQueryCallback1<T> {
    add(...callbacks: Array<{ (arg: T): void }>): JQueryCallback1<T>;
    add(callbacks: Array<{ (arg: T): void }>): JQueryCallback1<T>;
    disable(): JQueryCallback1<T>;
    disabled(): boolean;
    empty(): JQueryCallback1<T>;
    fire(arg: T): JQueryCallback1<T>;
    fired(): boolean;
    fireWith(context: any, args: any[]): JQueryCallback1<T>;
    has(callback: { (arg: T): void }): boolean;
    lock(): JQueryCallback1<T>;
    locked(): boolean;
    remove(...callbacks: Array<{ (arg: T): void }>): JQueryCallback1<T>;
    remove(callbacks: Array<{ (arg: T): void }>): JQueryCallback1<T>;
}

interface JQueryCallback2<T1, T2> {
    add(...callbacks: Array<{ (arg1: T1, arg2: T2): void }>): JQueryCallback2<T1, T2>;
    add(callbacks: Array<{ (arg1: T1, arg2: T2): void }>): JQueryCallback2<T1, T2>;
    disable(): JQueryCallback2<T1, T2>;
    disabled(): boolean;
    empty(): JQueryCallback2<T1, T2>;
    fire(arg1: T1, arg2: T2): JQueryCallback2<T1, T2>;
    fired(): boolean;
    fireWith(context: any, args: any[]): JQueryCallback2<T1, T2>;
    has(callback: { (arg1: T1, arg2: T2): void }): boolean;
    lock(): JQueryCallback2<T1, T2>;
    locked(): boolean;
    remove(...callbacks: Array<{ (arg1: T1, arg2: T2): void }>): JQueryCallback2<T1, T2>;
    remove(callbacks: Array<{ (arg1: T1, arg2: T2): void }>): JQueryCallback2<T1, T2>;
}

interface JQueryCallback3<T1, T2, T3> {
    add(...callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3): void }>): JQueryCallback3<T1, T2, T3>;
    add(callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3): void }>): JQueryCallback3<T1, T2, T3>;
    disable(): JQueryCallback3<T1, T2, T3>;
    disabled(): boolean;
    empty(): JQueryCallback3<T1, T2, T3>;
    fire(arg1: T1, arg2: T2, arg3: T3): JQueryCallback3<T1, T2, T3>;
    fired(): boolean;
    fireWith(context: any, args: any[]): JQueryCallback3<T1, T2, T3>;
    has(callback: { (arg1: T1, arg2: T2, arg3: T3): void }): boolean;
    lock(): JQueryCallback3<T1, T2, T3>;
    locked(): boolean;
    remove(...callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3): void }>): JQueryCallback3<T1, T2, T3>;
    remove(callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3): void }>): JQueryCallback3<T1, T2, T3>;
}

interface JQueryCallback4<T1, T2, T3, T4> {
    add(...callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3, arg4: T4): void }>): JQueryCallback4<T1, T2, T3, T4>;
    add(callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3, arg4: T4): void }>): JQueryCallback4<T1, T2, T3, T4>;
    disable(): JQueryCallback4<T1, T2, T3, T4>;
    disabled(): boolean;
    empty(): JQueryCallback4<T1, T2, T3, T4>;
    fire(arg1: T1, arg2: T2, arg3: T3, arg4: T4): JQueryCallback4<T1, T2, T3, T4>;
    fired(): boolean;
    fireWith(context: any, args: any[]): JQueryCallback4<T1, T2, T3, T4>;
    has(callback: { (arg1: T1, arg2: T2, arg3: T3, arg4: T4): void }): boolean;
    lock(): JQueryCallback4<T1, T2, T3, T4>;
    locked(): boolean;
    remove(...callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3, arg4: T4): void }>): JQueryCallback4<T1, T2, T3, T4>;
    remove(callbacks: Array<{ (arg1: T1, arg2: T2, arg3: T3, arg4: T4): void }>): JQueryCallback4<T1, T2, T3, T4>;
}

/*
    Interface for the JQuery promise, part of callbacks
*/
interface JQueryPromiseAny {
    always(...alwaysCallbacks: { (...args: any[]): void; }[]): JQueryPromiseAny;
    done(...doneCallbacks: { (...args: any[]): void; }[]): JQueryPromiseAny;
    fail(...failCallbacks: { (...args: any[]): void; }[]): JQueryPromiseAny;
    progress(...progressCallbacks: { (...args: any[]): void; }[]): JQueryPromiseAny;
    state(): string;
    promise(target?: any): JQueryPromiseAny;
    then(
        doneCallbacks: { (...args: any[]): any; },
        failCallbacks: { (...args: any[]): any; },
        progressCallbacks?: { (...args: any[]): any; }): JQueryPromiseAny;
}

interface JQueryPromise<TValue = any, TReject = any, TNotify = never> {
    always(...alwaysCallbacks: (() => void)[]): this;
    done(...doneCallbacks: ((value: TValue) => void)[]): this;
    fail(...failCallbacks: ((value: TReject) => void)[]): this;
    progress(...progressCallbacks: ((arg: TNotify) => void)[]): this;
    state(): string;
    promise(target?: any): JQueryPromise<TValue, TReject>;

    then<UValue = never, UReject = never>(
        doneCallbacks: (value: TValue) => UValue | JQueryPromise<UValue, UReject>
    ): JQueryPromiseVR<UValue, UReject>;

    then<UValue = never, UReject = never>(
        doneCallbacks: (value: TValue) => UValue,
        failCallbacks?: (reject: TReject) => UReject,
        progressCallbacks?: () => void
    ): JQueryPromiseVR<UValue, UReject>;

    catch(catchCallback: (reason?: any) => any): this;
    finally(finallyCallback: () => any): this;
}

type JQueryPromiseV<TValue> = JQueryPromise<TValue, any>;

type JQueryPromiseR<TReject> = JQueryPromise<any, TReject>;

type JQueryPromiseVR<TValue, TReject> = JQueryPromise<TValue, TReject>;

/*
    Interface for the JQuery deferred, part of callbacks
*/
interface JQueryDeferredAny {
    always(...alwaysCallbacks: { (...args: any[]): void; }[]): JQueryDeferredAny;
    done(...doneCallbacks: { (...args: any[]): void; }[]): JQueryDeferredAny;
    fail(...failCallbacks: { (...args: any[]): void; }[]): JQueryDeferredAny;
    progress(...progressCallbacks: { (): void; }[]): JQueryDeferredAny;
    notify(...args: any[]): JQueryDeferredAny;
    notifyWith(context: any, args: any[]): JQueryDeferredAny;
    promise(target?: any): JQueryPromiseAny;
    reject(...args: any[]): JQueryDeferredAny;
    rejectWith(context: any, args: any[]): JQueryDeferredAny;
    resolve(...args: any[]): JQueryDeferredAny;
    resolveWith(context: any, args: any[]): JQueryDeferredAny;
    state(): string;
    then(
        doneCallbacks: { (...args: any[]): any; },
        failCallbacks: { (...args: any[]): any; },
        progressCallbacks?: { (...args: any[]): any; }): JQueryDeferredAny;
}

interface JQueryDeferred<TValue = never, TReject = never, TNotify = never> extends JQueryPromise<TValue, TReject, TNotify> {
    notify(arg: TNotify): this;
    notifyWith(context: any, arg: TNotify): this;

    reject(reject?: TReject): this;
    rejectWith(context: any, args?: [TReject]): this;
    resolve(value?: TValue): this;
    resolveWith(context: any, args?: [TValue]): this;
}

type JQueryDeferredV<TValue> = JQueryDeferred<TValue, any>;

type JQueryDeferredR<TReject> = JQueryDeferred<any, TReject>;

type JQueryDeferredVR<TValue, TReject> = JQueryDeferred<TValue, TReject>;

type JQueryDeferredN<TNotify> = JQueryDeferred<never, never, TNotify>;

/*
    Interface of the JQuery extension of the W3C event object
*/
interface BaseJQueryEventObject extends Event {
    data: any;
    isDefaultPrevented(): boolean;
    isImmediatePropagationStopped(): boolean;
    isPropagationStopped(): boolean;
    originalEvent: Event;
    namespace: string;
    preventDefault(): any;
    result: any;
    stopImmediatePropagation(): void;
    stopPropagation(): void;
    pageX: number;
    pageY: number;
    which: number;

    // Other possible values
    cancellable?: boolean;
    // detail ??
    prevValue?: any;
    view?: Window;
}

interface JQueryInputEventObject extends BaseJQueryEventObject {
    altKey: boolean;
    ctrlKey: boolean;
    metaKey: boolean;
    shiftKey: boolean;
}

interface JQueryMouseEventObject extends JQueryInputEventObject {
    button: number;
    clientX: number;
    clientY: number;
    offsetX: number;
    offsetY: number;
    pageX: number;
    pageY: number;
    screenX: number;
    screenY: number;
}

interface JQueryKeyEventObject extends JQueryInputEventObject {
    char: any;
    charCode: number;
    key: any;
    keyCode: number;
}

interface JQueryEventObject extends BaseJQueryEventObject, JQueryInputEventObject, JQueryMouseEventObject, JQueryKeyEventObject {
}

interface JQueryEventHandler {
    (eventObject: JQueryEventObject, args?: any): any;
}


interface JQuerySupport {
    ajax?: boolean;
    boxModel?: boolean;
    changeBubbles?: boolean;
    checkClone?: boolean;
    checkOn?: boolean;
    cors?: boolean;
    cssFloat?: boolean;
    hrefNormalized?: boolean;
    htmlSerialize?: boolean;
    leadingWhitespace?: boolean;
    noCloneChecked?: boolean;
    noCloneEvent?: boolean;
    opacity?: boolean;
    optDisabled?: boolean;
    optSelected?: boolean;
    scriptEval?(): boolean;
    style?: boolean;
    submitBubbles?: boolean;
    tbody?: boolean;
}

// TODO jsgoupil fix signature
interface JQueryEventStatic {
    fix(evt: any): any;
}

interface JQueryParam {
    (obj: any): string;
    (obj: any, traditional: boolean): string;
}

/**
 * This is a private type. It exists for type checking. Do not explicitly declare an identifier with this type.
 */
interface _JQueryDeferred {
    resolve: Function;
    resolveWith: Function;
    reject: Function;
    rejectWith: Function;
}

interface JQueryWhen {
    (...deferreds: JQueryPromise[]): JQueryPromise;
    apply($: JQueryStatic, deferreds: JQueryPromise[]): JQueryPromise;
}

/*
    Static members of jQuery (those on $ and jQuery themselves)
*/
interface JQueryStatic {

    /****
     AJAX
    *****/
    ajax<T>(settings: JQueryAjaxSettings<T>): JQueryXHR<T>;
    ajax<T>(url: string, settings?: JQueryAjaxSettings<T>): JQueryXHR<T>;

    ajaxPrefilter(dataTypes: string, handler: (opts: any, originalOpts: any, jqXHR: JQueryXHR<any>) => any): any;
    ajaxPrefilter(handler: (opts: any, originalOpts: any, jqXHR: JQueryXHR<any>) => any): any;

    ajaxSettings: JQueryAjaxSettings<any>;

    ajaxSetup(options: JQueryAjaxSettings<any>): void;

    ajaxTransport<T>(dataType: string,
        handler: (options: JQueryAjaxSettings<T>, originalOptions: JQueryAjaxSettings<T>, jqXHR: JQueryXHR<T>) => JQueryTransport): any;

    get<T>(url: string, data?: any, success?: any, dataType?: any): JQueryXHR<T>;
    getJSON<T>(url: string, data?: any, success?: any): JQueryXHR<T>;
    getScript<T>(url: string, success?: any): JQueryXHR<T>;

    param: JQueryParam;

    post<T>(url: string, data?: any, success?: any, dataType?: any): JQueryXHR<T>;

    /*********
     CALLBACKS
    **********/
    Callbacks(flags?: string): JQueryCallback;
    Callbacks<T>(flags?: string): JQueryCallback1<T>;
    Callbacks<T1, T2>(flags?: string): JQueryCallback2<T1, T2>;
    Callbacks<T1, T2, T3>(flags?: string): JQueryCallback3<T1, T2, T3>;
    Callbacks<T1, T2, T3, T4>(flags?: string): JQueryCallback4<T1, T2, T3, T4>;

    /****
     CORE
    *****/
    holdReady(hold: boolean): any;

    (object: JQuery): JQuery;
    (func: Function): JQuery;
    (array: any[]): JQuery;
    (): JQuery;

    noConflict(removeAll?: boolean): Object;

    when: JQueryWhen;

    /***
     CSS
    ****/
    css(e: any, propertyName: string, value?: any): JQuery;
    css(e: any, propertyName: any, value?: any): JQuery;
    cssHooks: { [key: string]: any; };
    cssNumber: any;

    /*******
     EFFECTS
    ********/
    fx: { tick: () => void; interval: number; stop: () => void; speeds: { slow: number; fast: number; }; off: boolean; step: any; };

    /******
     EVENTS
    *******/
    proxy(fn: (...args: any[]) => any, context: any, ...args: any[]): any;
    proxy(context: any, name: string, ...args: any[]): any;
    Deferred: {
        (fn?: (d: JQueryDeferred) => void): JQueryDeferred;
        new(fn?: (d: JQueryDeferred) => void): JQueryDeferred;

        // Can't use a constraint against JQueryDeferred because the non-generic JQueryDeferred.resolve is not a base type of
        // the generic JQueryDeferred.resolve methods.
        <TDeferred extends _JQueryDeferred = JQueryDeferred>(fn?: (d: TDeferred) => void): TDeferred;
        new <TDeferred extends _JQueryDeferred = JQueryDeferred>(fn?: (d: TDeferred) => void): TDeferred;
    };
    Event(name: string, eventProperties?: any): JQueryEventObject;
    Event(evt: JQueryEventObject, eventProperties?: any): JQueryEventObject;

    event: JQueryEventStatic;

    /*********
     INTERNALS
    **********/
    error(message: any): JQuery;

    /*************
     MISCELLANEOUS
    **************/
    expr: any;
    fn: JQuery;
    isReady: boolean;

    /**********
     PROPERTIES
    ***********/
    support: JQuerySupport;

    /*********
     UTILITIES
    **********/
    each(collection: any, callback: (indexInArray: any, valueOfElement: any) => any): any;
    each<T>(collection: T[], callback: (indexInArray: number, valueOfElement: T) => void): T[];

    extend(deep: boolean, target: any, ...objs: any[]): any;
    extend(target: any, ...objs: any[]): any;

    globalEval(code: string): any;

    grep<T>(array: T[], func: (elementOfArray: T, indexInArray: number) => boolean, invert?: boolean): T[];

    inArray<T>(value: T, array: T[], fromIndex?: number): number;

    isArray(obj: any): boolean;
    isEmptyObject(obj: any): boolean;
    isFunction(obj: any): boolean;
    isNumeric(value: any): boolean;
    isPlainObject(obj: any): boolean;
    isWindow(obj: any): boolean;

    makeArray(obj: any): any[];

    map<T, U>(array: T[], callback: (elementOfArray: T, indexInArray: number) => U): U[];
    map<T, U>(object: { [item: string]: T; }, callback: (elementOfArray: T, indexInArray: string) => U): U[];
    map(array: any, callback: (elementOfArray: any, indexInArray: any) => any): any;

    merge<T>(first: T[], second: T[]): T[];

    noop(): any;

    now(): number;

    // BEGIN MODIFIED BY IBIZA
    // The commented function below is dangerous because it allows
    // $.parseHTML("<img src='' onerror='alert()'/>")
    // parseHTML(data: string, context?: Element, keepScripts?: boolean): Element[];
    // END MODIFIED BY IBIZA

    parseJSON(json: string): Object;

    trim(str: string): string;

    type(obj: any): string;

    unique<T>(arr: T[]): T[];
}

interface JQueryTransport {
    send(headers: { [index: string]: string; }, completeCallback: (status: number, statusText: string, responses?: { [dataType: string]: any; }, headers?: string) => any): any;
    abort(): any;
}

/*
    The jQuery instance members
*/
interface JQuery {
    /****
     AJAX
    *****/
    ajaxComplete(handler: any): JQuery;
    ajaxError(handler: (event: any, jqXHR: any, settings: any, exception: any) => any): JQuery;
    ajaxSend(handler: (event: any, jqXHR: any, settings: any, exception: any) => any): JQuery;
    ajaxStart(handler: () => any): JQuery;
    ajaxStop(handler: () => any): JQuery;
    ajaxSuccess(handler: (event: any, jqXHR: any, settings: any, exception: any) => any): JQuery;

    load(url: string, data?: any, complete?: any): JQuery;

    serialize(): string;
    serializeArray(): any[];

    /**********
     ATTRIBUTES
    ***********/
    addClass(classNames: string): JQuery;
    addClass(func: (index: any, currentClass: any) => string): JQuery;

    // http://api.jquery.com/addBack/
    addBack(selector?: string): JQuery;


    attr(attributeName: string): string;
    attr(attributeName: string, value: any): JQuery;
    attr(map: { [key: string]: any; }): JQuery;
    attr(attributeName: string, func: (index: any, attr: any) => any): JQuery;

    hasClass(className: string): boolean;

    html(): string;
    html(htmlString: number): JQuery;
    html(htmlString: string): JQuery;
    html(htmlContent: (index: number, oldhtml: string) => string): JQuery;

    prop(propertyName: string): any;
    prop(propertyName: string, value: any): JQuery;
    prop(map: any): JQuery;
    prop(propertyName: string, func: (index: any, oldPropertyValue: any) => any): JQuery;

    removeAttr(attributeName: any): JQuery;

    removeClass(className?: any): JQuery;
    removeClass(func: (index: any, cls: any) => any): JQuery;

    removeProp(propertyName: any): JQuery;

    toggleClass(className: any, swtch?: boolean): JQuery;
    toggleClass(swtch?: boolean): JQuery;
    toggleClass(func: (index: any, cls: any, swtch: any) => any): JQuery;

    val(): any;
    val(value: string[]): JQuery;
    val(value: string): JQuery;
    val(value: number): JQuery;
    val(func: (index: any, value: any) => any): JQuery;

    /***
     CSS
    ****/
    css(propertyName: string): string;
    css(propertyNames: string[]): string;
    css(properties: any): JQuery;
    css(propertyName: string, value: any): JQuery;
    css(propertyName: any, value: any): JQuery;

    height(): number;
    height(value: number): JQuery;
    height(value: string): JQuery;
    height(func: (index: any, height: any) => any): JQuery;

    innerHeight(): number;
    innerWidth(): number;

    offset(): { left: number; top: number; };
    offset(coordinates: any): JQuery;
    offset(func: (index: any, coords: any) => any): JQuery;

    outerHeight(includeMargin?: boolean): number;
    outerWidth(includeMargin?: boolean): number;

    position(): { top: number; left: number; };

    scrollLeft(): number;
    scrollLeft(value: number): JQuery;

    scrollTop(): number;
    scrollTop(value: number): JQuery;

    width(): number;
    width(value: number): JQuery;
    width(value: string): JQuery;
    width(func: (index: any, height: any) => any): JQuery;

    /****
     DATA
    *****/
    clearQueue(queueName?: string): JQuery;

    data(key: string, value: any): JQuery;
    data(key: string): any;
    data(obj: { [key: string]: any; }): JQuery;
    data(): any;

    dequeue(queueName?: string): JQuery;

    removeData(nameOrList?: any): JQuery;

    /********
     DEFERRED
    *********/
    promise(type?: any, target?: any): JQueryPromise;

    /*******
     EFFECTS
    ********/
    animate(properties: any, duration?: any, complete?: Function): JQuery;
    animate(properties: any, duration?: any, easing?: string, complete?: Function): JQuery;
    animate(properties: any, options: { duration?: any; easing?: string; complete?: Function; step?: Function; queue?: boolean; specialEasing?: any; }): JQuery;

    delay(duration: number, queueName?: string): JQuery;

    fadeIn(duration?: any, callback?: any): JQuery;
    fadeIn(duration?: any, easing?: string, callback?: any): JQuery;

    fadeOut(duration?: any, callback?: any): JQuery;
    fadeOut(duration?: any, easing?: string, callback?: any): JQuery;

    fadeTo(duration: any, opacity: number, callback?: any): JQuery;
    fadeTo(duration: any, opacity: number, easing?: string, callback?: any): JQuery;

    fadeToggle(duration?: any, callback?: any): JQuery;
    fadeToggle(duration?: any, easing?: string, callback?: any): JQuery;

    finish(): JQuery;

    /****
     * Performance pitfall: Every hide/show force browser render now on the spot.
     * Remove .show() , .hide()  and.toggle()
     * If you need to do simple hide, show.  Please use JQueryHelper.hide and JQueryHelper.show which do the simple
     * .addClass("fxs-display-none") -- for hide() with display:none !important
     * .removeClass("fxs-display-none") -- for show()
     * Note that current JQuery not only slow.  There are cases that it failed to remove inline style class
     * Reference  Chrome Debugger team PM blog: https://twitter.com/paul_irish/status/564443848613847040?lang=en

    hide(duration?: any, callback?: any): JQuery;
    hide(duration?: any, easing?: string, callback?: any): JQuery;

    show(duration?: any, callback?: any): JQuery;
    show(duration?: any, easing?: string, callback?: any): JQuery;
     */

    slideDown(duration?: any, callback?: any): JQuery;
    slideDown(duration?: any, easing?: string, callback?: any): JQuery;

    slideToggle(duration?: any, callback?: any): JQuery;
    slideToggle(duration?: any, easing?: string, callback?: any): JQuery;

    slideUp(duration?: any, callback?: any): JQuery;
    slideUp(duration?: any, easing?: string, callback?: any): JQuery;

    stop(clearQueue?: boolean, jumpToEnd?: boolean): JQuery;
    stop(queue?: any, clearQueue?: boolean, jumpToEnd?: boolean): JQuery;

    /***
      * Performance pitfall: Every hide/show force browser render now on the spot.
      * Remove .show() , .hide() and .toggle()
      * If you need to do simple hide, show.  Please use JQueryHelper.toggle which do the simple
      * .addClass("fxs-display-none") -- for hide() with display:none !important
      * .removeClass("fxs-display-none") -- for show()
      * Note that current JQuery not only slow.  There are cases that it failed to remove inline style class
      * Reference  Chrome Debugger team PM blog: https://twitter.com/paul_irish/status/564443848613847040?lang=en

     toggle(duration?: any, callback?: any): JQuery;
     toggle(duration?: any, easing?: string, callback?: any): JQuery;
     toggle(showOrHide: boolean): JQuery;
     ***/

    /******
     EVENTS
    *******/
    bind(eventType: string, eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    bind(eventType: string, eventData: any, preventBubble: boolean): JQuery;
    bind(eventType: string, preventBubble: boolean): JQuery;
    bind(...events: any[]): JQuery;

    blur(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    blur(handler: (eventObject: JQueryEventObject) => any): JQuery;

    change(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    change(handler: (eventObject: JQueryEventObject) => any): JQuery;

    click(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    click(handler: (eventObject: JQueryEventObject) => any): JQuery;

    dblclick(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    dblclick(handler: (eventObject: JQueryEventObject) => any): JQuery;

    delegate(selector: any, eventType: string, handler: (eventObject: JQueryEventObject) => any): JQuery;

    focus(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    focus(handler: (eventObject: JQueryEventObject) => any): JQuery;

    focusin(eventData: any, handler: (eventObject: JQueryEventObject) => any): JQuery;
    focusin(handler: (eventObject: JQueryEventObject) => any): JQuery;

    focusout(eventData: any, handler: (eventObject: JQueryEventObject) => any): JQuery;
    focusout(handler: (eventObject: JQueryEventObject) => any): JQuery;

    hover(handlerIn: (eventObject: JQueryEventObject) => any, handlerOut: (eventObject: JQueryEventObject) => any): JQuery;
    hover(handlerInOut: (eventObject: JQueryEventObject) => any): JQuery;

    keydown(eventData?: any, handler?: (eventObject: JQueryKeyEventObject) => any): JQuery;
    keydown(handler: (eventObject: JQueryKeyEventObject) => any): JQuery;

    keypress(eventData?: any, handler?: (eventObject: JQueryKeyEventObject) => any): JQuery;
    keypress(handler: (eventObject: JQueryKeyEventObject) => any): JQuery;

    keyup(eventData?: any, handler?: (eventObject: JQueryKeyEventObject) => any): JQuery;
    keyup(handler: (eventObject: JQueryKeyEventObject) => any): JQuery;

    load(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    load(handler: (eventObject: JQueryEventObject) => any): JQuery;

    mousedown(): JQuery;
    mousedown(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mousedown(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mouseevent(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mouseevent(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mouseenter(): JQuery;
    mouseenter(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mouseenter(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mouseleave(): JQuery;
    mouseleave(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mouseleave(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mousemove(): JQuery;
    mousemove(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mousemove(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mouseout(): JQuery;
    mouseout(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mouseout(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mouseover(): JQuery;
    mouseover(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mouseover(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    mouseup(): JQuery;
    mouseup(eventData: any, handler: (eventObject: JQueryMouseEventObject) => any): JQuery;
    mouseup(handler: (eventObject: JQueryMouseEventObject) => any): JQuery;

    off(events?: string, selector?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    off(eventsMap: { [key: string]: any; }, selector?: any): JQuery;

    on(events: string, selector: any, data: any, handler: (eventObject: JQueryEventObject, args: any) => any): JQuery;
    on(events: string, selector: any, handler: (eventObject: JQueryEventObject) => any): JQuery;
    on(events: string, handler: (eventObject: JQueryEventObject, args: any) => any): JQuery;
    on(eventsMap: { [key: string]: any; }, selector?: any, data?: any): JQuery;

    one(events: string, selector?: any, data?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    one(eventsMap: { [key: string]: any; }, selector?: any, data?: any): JQuery;

    ready(handler: any): JQuery;

    resize(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    resize(handler: (eventObject: JQueryEventObject) => any): JQuery;

    scroll(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    scroll(handler: (eventObject: JQueryEventObject) => any): JQuery;

    select(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    select(handler: (eventObject: JQueryEventObject) => any): JQuery;

    submit(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    submit(handler: (eventObject: JQueryEventObject) => any): JQuery;

    trigger(eventType: string, ...extraParameters: any[]): JQuery;
    trigger(event: JQueryEventObject, ...extraParameters: any[]): JQuery;

    triggerHandler(eventType: string, ...extraParameters: any[]): Object;
    // JSGOUPIL: triggerHandler uses trigger, not documented though
    triggerHandler(evt: JQueryEventObject): Object;

    unbind(eventType?: string, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    unbind(eventType: string, fls: boolean): JQuery;
    unbind(evt: any): JQuery;

    undelegate(): JQuery;
    undelegate(selector: any, eventType: string, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    undelegate(selector: any, events: any): JQuery;
    undelegate(namespace: string): JQuery;

    unload(eventData?: any, handler?: (eventObject: JQueryEventObject) => any): JQuery;
    unload(handler: (eventObject: JQueryEventObject) => any): JQuery;

    /*********
     INTERNALS
    **********/

    jquery: string;

    error(handler: (eventObject: JQueryEventObject) => any): JQuery;
    error(eventData: any, handler: (eventObject: JQueryEventObject) => any): JQuery;

    pushStack(elements: any[]): JQuery;
    pushStack(elements: any[], name: any, arguments: any): JQuery;

    /************
     MANIPULATION
    *************/
    after(...content: any[]): JQuery;
    after(func: (index: any) => any): JQuery;

    // BEGIN MODIFIED BY IBIZA
    // The commented signature below is dangerous because it allows
    // $(document.body).append("<img src='' onerror='alert()'/>")
    // append(...content: any[]): JQuery;
    // END MODIFIED BY IBIZA
    append(func: (index: any, html: any) => any): JQuery;

    appendTo(target: any): JQuery;

    before(...content: any[]): JQuery;
    before(func: (index: any) => any): JQuery;

    clone(withDataAndEvents?: boolean, deepWithDataAndEvents?: boolean): JQuery;

    detach(selector?: any): JQuery;

    empty(): JQuery;

    insertAfter(target: any): JQuery;
    insertBefore(target: any): JQuery;

    prepend(func: (index: any, html: any) => any): JQuery;

    prependTo(target: any): JQuery;

    remove(selector?: any): JQuery;

    replaceAll(target: any): JQuery;

    replaceWith(func: any): JQuery;

    text(): string;
    text(textString: any): JQuery;
    text(textString: (index: number, text: string) => string): JQuery;

    toArray(): any[];

    unwrap(): JQuery;

    wrap(func: (index: any) => any): JQuery;

    wrapAll(wrappingElement: any): JQuery;

    wrapInner(wrappingElement: any): JQuery;
    wrapInner(func: (index: any) => any): JQuery;

    /*************
     MISCELLANEOUS
    **************/

    get(index?: number): any;

    index(): number;
    index(selector: string): number;
    index(element: any): number;

    /**********
     PROPERTIES
    ***********/
    length: number;
    selector: string;
    [x: string]: any;

    /**********
     TRAVERSING
    ***********/
    add(selector: string, context?: any): JQuery;
    add(...elements: any[]): JQuery;
    add(html: string): JQuery;
    add(obj: JQuery): JQuery;

    children(selector?: any): JQuery;

    closest(selector: string): JQuery;
    closest(obj: JQuery): JQuery;
    closest(element: any): JQuery;

    contents(): JQuery;

    end(): JQuery;

    eq(index: number): JQuery;

    filter(selector: string): JQuery;
    filter(func: (index: any, element: any) => any): JQuery;
    filter(element: any): JQuery;
    filter(obj: JQuery): JQuery;

    find(selector: string): JQuery;
    find(element: any): JQuery;
    find(obj: JQuery): JQuery;

    first(): JQuery;

    has(selector: string): JQuery;

    is(selector: string): boolean;
    is(func: (index: any) => any): boolean;
    is(element: any): boolean;
    is(obj: JQuery): boolean;

    last(): JQuery;

    next(selector?: string): JQuery;

    nextAll(selector?: string): JQuery;

    nextUntil(selector?: string, filter?: string): JQuery;

    not(selector: string): JQuery;
    not(func: (index: any) => any): JQuery;
    not(element: any): JQuery;
    not(obj: JQuery): JQuery;

    offsetParent(): JQuery;

    parent(selector?: string): JQuery;

    parents(selector?: string): JQuery;

    parentsUntil(selector?: string, filter?: string): JQuery;

    prev(selector?: string): JQuery;

    prevAll(selector?: string): JQuery;

    prevUntil(selector?: string, filter?: string): JQuery;

    siblings(selector?: string): JQuery;

    slice(start: number, end?: number): JQuery;

    /*********
     UTILITIES
    **********/

    queue(queueName?: string): any[];
    queue(queueName: string, newQueueOrCallback: any): JQuery;
    queue(newQueueOrCallback: any): JQuery;
}

declare var jQuery: JQueryStatic;
declare var $: JQueryStatic;
