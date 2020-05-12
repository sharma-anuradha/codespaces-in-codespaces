
export const initHostingHtmlTags = async () => {
    // initialize fabric icons
    const initializeIconsModule = await import('@uifabric/icons');
    initializeIconsModule.initializeIcons();
    // add fabric styles
    const style = document.createElement('link');
    style.setAttribute('rel', 'stylesheet');
    style.setAttribute('href', 'https://static2.sharepointonline.com/files/fabric/office-ui-fabric-core/10.0.0/css/fabric.min.css');
    document.head.appendChild(style);
};
