mergeInto(LibraryManager.library, {

    SendMessageJS: function (pidPtr, dataPtr, actionPtr)
    {
        var pid = UTF8ToString(pidPtr);
        var data = UTF8ToString(dataPtr);
        var action = UTF8ToString(actionPtr);

        UnityAO.sendMessage(pid, data, action);
    },

    SendMessageCustomCallbackJS: function (pidPtr, dataPtr, actionPtr, idPtr, objectPtr, methodPtr) {
        var pid = UTF8ToString(pidPtr);
        var data = UTF8ToString(dataPtr);
        var action = UTF8ToString(actionPtr);
        var id = UTF8ToString(idPtr);
        var object = UTF8ToString(objectPtr);
        var method = UTF8ToString(methodPtr);

        UnityAO.sendMessageCustomCallback(pid, data, action, id, object, method);
    },

    TransferTokenJS: function (pidPtr, quantityPtr, recipientPtr) {
        var pid = UTF8ToString(pidPtr);
        var quantity = UTF8ToString(quantityPtr);
        var recipient = UTF8ToString(recipientPtr);

        UnityAO.transferToken(pid, quantity, recipient);
    },

    ConnectWalletJS: function ()
    {
        UnityAO.connectArweaveWallet();
    },

    ConnectMetamaskJS: function ()
    {
        UnityAO.connectMetamaskWallet();
    },

    FetchProcessesJS: function (addressPtr)
    {
        var address = UTF8ToString(addressPtr);
        
        UnityAO.fetchProcesses(address);
    },

    SpawnProcessJS: function (pidPtr)
    {
        var pid = UTF8ToString(pidPtr);

        UnityAO.spawnProcess(pid);
    },

    LocalCuRegisterJS: function (pidPtr)
    {
        var pid = UTF8ToString(pidPtr);

        UnityAO.localCuRegister(pid);
    },

    LocalCuEvalJS: function (pidPtr, ownerPtr, actionPtr, dataPtr)
    {
        var pid = UTF8ToString(pidPtr);
        var owner = UTF8ToString(ownerPtr);
        var action = UTF8ToString(actionPtr);
        var data = UTF8ToString(dataPtr);

        UnityAO.localCuEvaluate(pid, owner, action, data);
    },

    SendSuJS: function (urlPtr, pidPtr, ownerPtr, actionPtr, dataPtr)
    {
        var url = UTF8ToString(urlPtr);
        var pid = UTF8ToString(pidPtr);
        var owner = UTF8ToString(ownerPtr);
        var action = UTF8ToString(actionPtr);
        var data = UTF8ToString(dataPtr);

        UnityAO.sendSU(url, pid, owner, action, data);
    }
});

