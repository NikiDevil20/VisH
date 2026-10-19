let viewer;
let currentData = null;
let cubeCache = new Map();
let orbitalModeRequest = 0;

function initialize() {
    viewer = $3Dmol.createViewer("viewer", { backgroundColor: "white" });
}

function showMessage(message) {
    document.getElementById("message").textContent = message || "";
}

function appendMessage(message) {
    if (!message) return;
    const element = document.getElementById("message");
    element.textContent = element.textContent
        ? `${element.textContent} ${message}`
        : message;
}

async function loadCube(url) {
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error(`Cube-Datei konnte nicht geladen werden (${response.status}).`);
    }
    return response.text();
}

function renderGeometry(data) {
    viewer.removeAllModels();
    viewer.removeAllSurfaces();
    viewer.addModel(data.geometry, "xyz");
    viewer.setStyle({}, { stick: {}, sphere: { scale: 0.28 } });
    viewer.zoomTo();
    viewer.render();
}

async function setMoleculeData(data) {
    orbitalModeRequest++;
    currentData = data;
    cubeCache = new Map();
    try {
        if (viewer) {
            viewer.removeAllModels();
            viewer.removeAllSurfaces();
        }
        initialize();
        renderGeometry(data);
    } catch (error) {
        showMessage(`Geometrie konnte nicht dargestellt werden: ${error.message}`);
        return;
    }

    showMessage(data.statusMessage || "");
    await renderOrbitalMode(data.orbitalMode || "HOMO");
}

async function getCube(url) {
    if (!cubeCache.has(url)) {
        cubeCache.set(url, await loadCube(url));
    }
    return cubeCache.get(url);
}

async function renderOrbitalMode(mode) {
    if (!currentData || !viewer) return;
    const request = ++orbitalModeRequest;
    viewer.removeAllSurfaces();
    showMessage(currentData.statusMessage || "");
    const selected = mode === "Beide"
        ? [["HOMO", currentData.homoCube], ["LUMO", currentData.lumoCube]]
        : mode === "LUMO"
            ? [["LUMO", currentData.lumoCube]]
            : [["HOMO", currentData.homoCube]];

    for (const [name, url] of selected) {
        if (request !== orbitalModeRequest) return;
        if (!url) {
            appendMessage(`${name}-Cube ist nicht verfügbar.`);
            continue;
        }
        await renderCubeSafely(
            () => renderOrbital(
                url,
                "#3f6dcc",
                "#d64b4b",
                currentData.isoValue,
                currentData.opacity,
                request),
            name);
    }
    if (request !== orbitalModeRequest) return;
    if (currentData.densityCube) {
        await renderCubeSafely(
            () => renderDensity(
                currentData.densityCube,
                currentData.isoValue,
                currentData.opacity,
                request),
            "Elektronendichte");
    }
    if (request !== orbitalModeRequest) return;
    viewer.render();
}

async function renderCubeSafely(render, name) {
    try {
        await render();
    } catch (error) {
        appendMessage(`${name}-Cube konnte nicht dargestellt werden: ${error.message}`);
    }
}

async function renderOrbital(
    url,
    positiveColor,
    negativeColor,
    isoValue,
    opacity,
    request) {
    const cube = await getCube(url);
    if (request !== orbitalModeRequest) return;
    viewer.addVolumetricData(cube, "cube", {
        isoval: isoValue,
        color: positiveColor,
        opacity: opacity
    });
    if (request !== orbitalModeRequest) {
        return;
    }
    viewer.addVolumetricData(cube, "cube", {
        isoval: -isoValue,
        color: negativeColor,
        opacity: opacity
    });
    if (request !== orbitalModeRequest) {
        return;
    }
    viewer.render();
}

async function renderDensity(url, isoValue, opacity, request) {
    const cube = await getCube(url);
    if (request !== orbitalModeRequest) return;
    viewer.addVolumetricData(cube, "cube", {
        isoval: isoValue,
        color: "#7b4ab5",
        opacity: opacity
    });
    if (request !== orbitalModeRequest) {
        return;
    }
    viewer.render();
}

window.setMoleculeData = setMoleculeData;
window.setOrbitalMode = async function (mode) {
    if (!currentData) return;
    await setMoleculeData({ ...currentData, orbitalMode: mode });
};
window.clearMoleculeData = function () {
    if (viewer) {
        viewer.removeAllModels();
        viewer.removeAllSurfaces();
        viewer.render();
    }
    cubeCache = new Map();
    currentData = null;
    orbitalModeRequest++;
    showMessage("");
};
