import sys
from rdkit import Chem
from rdkit.Chem import rdDepictor, Draw

smiles_string = sys.argv[1]
directory = sys.argv[2]

def draw_structure_to_png(smiles: str, path: str) -> None:
    mol = Chem.MolFromSmiles(smiles)
    rdDepictor.Compute2DCoords(mol)

    drawer = Draw.MolDraw2DSVG(400, 300)
    opts = drawer.drawOptions()

    opts.useBWAtomPalette()
    opts.clearBackground = True

    drawer.DrawMolecule(mol)
    drawer.FinishDrawing()

    with open(path, "w", encoding="utf-8") as f:
        f.write(drawer.GetDrawingText())
        
path = directory + "/structure.svg"

draw_structure_to_png(smiles_string, path)