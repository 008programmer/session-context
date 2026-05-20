let dotNet;

export function init(dotNetRef) {
    dotNet = dotNetRef;
}

export async function copyText(text) {
    let ok = false;
    try {
        await navigator.clipboard.writeText(text);
        ok = true;
    } catch {
        ok = false;
    }
    if (dotNet) {
        await dotNet.invokeMethodAsync("OnCopyResult", ok);
    }
}
