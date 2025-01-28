export function requestNotificationPermission() {
    // Check if the browser supports notifications
    if ("Notification" in window) {
        Notification.requestPermission().then(function (result) {
            console.log("Notification permission: ", result);
        });
    }
}

export function sendNotification(title, text) {
    // Ensure that the browser supports notifications
    if (!("Notification" in window)) {
        alert("This browser does not support desktop notification");
    }
    // Check whether notification permissions have already been granted
    else if (Notification.permission === "granted") {
        // If it's okay, let's create a notification
        var notification = new Notification(title, { body: text });
        console.log("Notification sent");
    }
    // Otherwise, we need to ask the user for permission
    else if (Notification.permission !== 'denied') {
        Notification.requestPermission(function (permission) {
            // If the user accepts, let's create a notification
            if (permission === "granted") {
                var notification = new Notification(title, { body: text });
                console.log("Notification sent");
            }
        });
    }
}
