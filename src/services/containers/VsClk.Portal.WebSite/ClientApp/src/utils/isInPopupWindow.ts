export const isInPopupWindow = (): boolean => {
    return !!(window.opener && window.opener !== window);
};
