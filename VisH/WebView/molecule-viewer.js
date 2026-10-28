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

function getDisplayMode(data) {
    return data?.displayMode || data?.orbitalMode || "HOMO";
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
    viewer.removeAllLabels?.();
    viewer.addModel(data.geometry, "xyz");
    viewer.setStyle({}, { stick: {}, sphere: { scale: 0.28 } });
    viewer.zoomTo();
    viewer.render();
}

function renderAtomIndices() {
    if (!currentData || !viewer) return;
    viewer.removeAllLabels?.();
    const lines = String(currentData.geometry || "").split(/\r?\n/);
    const atomCount = Number.parseInt(lines[0] || "0", 10);
    if (!Number.isFinite(atomCount) || atomCount <= 0) {
        return;
    }

    for (let i = 0; i < atomCount; i++) {
        const lineIndex = i + 2;
        const line = (lines[lineIndex] || "").trim();
        if (!line) continue;
        const parts = line.split(/\s+/).filter(Boolean);
        if (parts.length < 4) continue;

        const x = Number.parseFloat(parts[1]);
        const y = Number.parseFloat(parts[2]);
        const z = Number.parseFloat(parts[3]);
        if (!Number.isFinite(x) || !Number.isFinite(y) || !Number.isFinite(z)) {
            continue;
        }

        viewer.addLabel(String(i + 1), {
            position: { x, y, z },
            alignment: "center",
            showBackground: false,
            fontSize: 12,
            fontColor: "black",
            useScreen: false,
            inFront: true
        });
    }
    viewer.render();
}

async function setMoleculeData(data) {
    orbitalModeRequest++;
    currentData = { ...data, displayMode: getDisplayMode(data) };
    cubeCache = new Map();
    try {
        if (viewer) {
            viewer.removeAllModels();
            viewer.removeAllSurfaces();
            viewer.removeAllLabels?.();
        }
        initialize();
        renderGeometry(currentData);
    } catch (error) {
        showMessage(`Geometrie konnte nicht dargestellt werden: ${error.message}`);
        return;
    }

    showMessage(currentData.statusMessage || "");
    if (currentData.displayMode === "Indices") {
        renderAtomIndices();
        return;
    }
    await renderOrbitalMode(currentData.displayMode || "HOMO");
}

async function getCube(url) {
    if (!cubeCache.has(url)) {
        cubeCache.set(url, await loadCube(url));
    }
    return cubeCache.get(url);
}

async function renderOrbitalMode(mode) {
    if (!currentData || !viewer) return;
    if (mode === "Indices") {
        viewer.removeAllSurfaces();
        viewer.removeAllLabels?.();
        renderAtomIndices();
        return;
    }

    const request = ++orbitalModeRequest;
    viewer.removeAllSurfaces();
    viewer.removeAllLabels?.();
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
window.setDisplayMode = async function (mode) {
    if (!currentData) return;
    await setMoleculeData({ ...currentData, displayMode: mode, orbitalMode: mode === "Indices" ? "HOMO" : mode });
};
window.setOrbitalMode = async function (mode) {
    if (!currentData) return;
    await setMoleculeData({ ...currentData, displayMode: mode === "Indices" ? "Indices" : (currentData.displayMode === "Indices" ? "HOMO" : currentData.displayMode), orbitalMode: mode });
};
window.clearMoleculeData = function () {
    if (viewer) {
        viewer.removeAllModels();
        viewer.removeAllSurfaces();
        viewer.removeAllLabels?.();
        viewer.render();
    }
    cubeCache = new Map();
    currentData = null;
    orbitalModeRequest++;
    showMessage("");
};
