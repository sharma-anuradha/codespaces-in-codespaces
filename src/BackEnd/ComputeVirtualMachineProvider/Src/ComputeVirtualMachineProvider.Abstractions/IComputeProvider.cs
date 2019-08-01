using System.Threading.Tasks;
using ComputeVirtualMachineProvider.Models;

namespace StorageFileShareProvider.Abstractions
{
    public interface IComputeProvider
    {
        /// <summary>
        /// NOTE, this won't wait for Create workflow to finish
        /// this will return as soon as tracking info is ready.
        /// </summary>
        /// <param name="input">Provides input to Create Azure file share.</param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Create operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, string continuationToken = null);

        /// <summary>
        /// NOTE, this won't wait for Delete workflow to finish
        /// /// /// this will return as soon as tracking info is ready.
        /// </summary>
        /// <param name="input">Provides input to Delete Azure file share.</param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, string continuationToken = null);

        /// <summary>
        /// Prep VM to create user environment
        /// </summary>
        /// <param name="input"></param>
        /// <param name="continuationToken"></param>
        /// <returns>
        ///     Result of the Delete operations which includes TrackingId which
        ///     can be used to call the StatusCheckAsync to find out the status
        ///     of the create request.
        /// </returns>
        Task<VirtualMachineProviderAssignResult> AssignAsync(VirtualMachineProviderAssignInput input, string continuationToken = null);

    }
}


/*

SKU CATALOG DEFINITION EXAMPLE

'CE_Small_Linux_1' ->
    Storage - Premium, 8GB
    Compute Family - F Series
    Compute Sku - FS4_v3
    CEUnits - 10
    OS - Linux
'CE_Medium_Linux_1' ->
    Storage - Premium, 32GB
    Compute Family - F Series
    Compute Sku - FS16_v3
    CEUnits - 20
    OS - Linux
'CE_Large_Linux_1' ->
    Storage - Premium, 64GB
    Compute Family - F Series
    Compute Sku - FS32_v3
    CEUnits - 30
    OS - Linux


*/
