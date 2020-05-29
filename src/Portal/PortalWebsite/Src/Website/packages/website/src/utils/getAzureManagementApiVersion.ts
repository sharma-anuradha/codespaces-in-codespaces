/**
 * API Version	Endpoint URL
 * 2019-07-01-preview	            online.visualstudio.com/api/v1
 * 2019-07-01-beta	                online-ppe.vsengsaas.visualstudio.com/api/v1
 * 2019-07-01-alpha	                online.dev.vsengsaas.visualstudio.com/api/v1
 * 2019-07-01-privatepreview	    canary.online.visualstudio.com/api/v1
 */
export const getAzureManagementApiVersion = (): string => {
    const baseURL = window.location.href.split('/')[2];
    let apiVersion;
    if (baseURL.includes('dev')) {
        apiVersion = '2019-07-01-alpha';
    } else if (baseURL.includes('ppe')) {
        apiVersion = '2019-07-01-beta';
    } else if (baseURL.includes('canary')) {
        apiVersion = '2019-07-01-privatepreview';
    } else {
        apiVersion = '2019-07-01-preview';
    }
    return apiVersion;
};
