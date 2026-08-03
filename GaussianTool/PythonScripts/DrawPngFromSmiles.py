import sys

from rdkit import Chem
from rdkit.Chem import Draw, rdDepictor


def draw_structure_to_png(smiles: str, path: str) -> None:
    mol = Chem.MolFromSmiles(smiles)

    if mol is None:
        raise ValueError(f"Ungültiges SMILES: {smiles}")

    # 2D-Koordinaten erzeugen
    rdDepictor.Compute2DCoords(mol)

    # PNG-Drawer mit 400x150 px
    drawer = Draw.MolDraw2DCairo(400, 150)

    opts = drawer.drawOptions()

    # Schwarz-weiß Darstellung
    opts.useBWAtomPalette()

    # weißer Hintergrund
    opts.clearBackground = True

    # Molekül zeichnen
    drawer.DrawMolecule(mol)
    drawer.FinishDrawing()

    # PNG speichern
    with open(path, "wb") as f:
        f.write(drawer.GetDrawingText())

smiles_string = sys.argv[1]
directory = sys.argv[2]

path = directory + "/structure.png"
draw_structure_to_png(smiles_string, path)
