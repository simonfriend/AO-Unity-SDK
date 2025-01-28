 import SimplePeer from 'simple-peer';

window.WebRTCManager = {
    peers: {},

    initializePeer: function (playerId, isInitiator) {
        var peer = new SimplePeer({
            initiator: !!isInitiator,
            trickle: false
        });

        peer.on('signal', function (data) {
            unityInstance.SendMessage('WebRTCManager', 'OnSignal', JSON.stringify({ playerId: playerId, signal: data }));
        });

        peer.on('data', function (data) {
            unityInstance.SendMessage('WebRTCManager', 'OnDataReceived', data);
        });

        peer.on('connect', function () {
            unityInstance.SendMessage('WebRTCManager', 'OnConnect', '');
        });

        peer.on('close', function () {
            unityInstance.SendMessage('WebRTCManager', 'OnDisconnect', playerId);
        });

        this.peers[playerId] = peer;
    },

    sendSignal: function (playerId, signalData) {
        var peer = this.peers[playerId];
        if (peer) {
            peer.signal(JSON.parse(signalData));
        } else {
            console.error("Peer connection not found for playerId: " + playerId);
        }
    },

    sendData: function (playerId, data) {
        var peer = this.peers[playerId];
        if (peer) {
            peer.send(data);
        } else {
            console.error("Peer connection not found for playerId: " + playerId);
        }
    }
};

