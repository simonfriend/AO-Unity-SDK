//COMMENTED THIS SINCE WE ARE NOT USING THEM FOR THE MOMENT!! CAMMELLO
// import * as Comlink from "https://unpkg.com/comlink/dist/esm/comlink.mjs";
// const worker = Comlink.wrap(new Worker("worker.js"));
const worker = "worker.js";
const hardcodedProcessId = "sYJP_BK4fwXNl0rg8J_5gUWZfmAitoXh_GVleAyGFKg";
// const hardcodedAopModuleTxId = "KQmGe8mHG8cV2Bx553ZVncaSf6JQZuHcfh9UwGfCmtw"; zV_TXVJ6YYzpnXazszcSzkLrJ1N1T-qHU4gYU0FLtu8
const hardcodedModuleTxId = "BWgrjsYKFXLdlh6-zmV9AKkv-If4pemkVIHCetHSBDM";
let count = 0;

export async function localCuRegister(processId) {
  // const loadResult = await worker.loadVirtualAopProcess();
  const loadResult = await worker.loadProcess(processId);
  const handleResult = await worker.createHandle();

  myUnityInstance.SendMessage("AONetworkManager", "RegisterCallback", "ok");

  return {
    loadResult,
    handleResult,
  }
}

export async function localCuEvaluate(processId, owner, action, data) {

  const message = createMessage(processId, owner, action, data);
  count++;
  // console.log(message);

  let json;

  const result = await worker.applyMessage(message)

  if (result && result.Memory) {
    delete result.Memory;
  }

  // console.log(result);

  json = JSON.stringify(result);
  myUnityInstance.SendMessage("AONetworkManager", "MessageCallback", json);

  return result;
}

export function createMessage(processId, owner, action, data) {
  return {
    // process: processId,
    // Process: processId,
    Target: processId,
    Owner: owner,
    From: owner,
    "Block-Height": "0",
    Id: "MessageID_" + count,
    Module: "BWgrjsYKFXLdlh6-zmV9AKkv-If4pemkVIHCetHSBDM",
    Tags: [
      { name: "Action", value: action },
      // { name: "Nonce", value: Math.random().toString() },
      {
        name: "Data-Protocol",
        value: "ao",
      },
      {
        name: "Type",
        value: "Message",
      },
      {
        name: "Variant",
        value: "ao.TN.1",
      },
    ],
    Data: data,
    Timestamp: Date.now().toString(),
    Cron: false,
  };
}

