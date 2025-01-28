mergeInto(LibraryManager.library, {
    GetURLFromQueryStr: function () {
        var returnStr = window.top.location.href;
        var bufferSize = lengthBytesUTF8(returnStr) + 1
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    RefreshPage: function () {
        document.location.reload(true);
    },

    AlertMessageJS: function (messagePtr) {
        var message = UTF8ToString(messagePtr);

        alert(message);
    },

    CheckNotificationPermissionJS: function () {

        UnityAO.requestNotificationPermission();
    },

    SendNotificationJS: function (titlePtr, textPtr) {
        var title = UTF8ToString(titlePtr);
        var text = UTF8ToString(textPtr);

        UnityAO.sendNotification(title, text);
    },

    TwitterShareJS: function (textPtr) {
        var text = UTF8ToString(textPtr);

        UnityAO.shareOnTwitter(text);
    },

    DownloadImageJS: function (dataPtr, filenamePtr) {
        var data = UTF8ToString(dataPtr);
        var filename = UTF8ToString(filenamePtr);

        UnityAO.downloadImage(data, filename);
    }

});
