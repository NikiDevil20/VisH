import sys
import json
from rdkit import Chem
from rdkit.Chem import AllChem, Draw, rdDepictor

smiles_string = sys.argv[1]

mol = Chem.MolFromSmiles(smiles_string)

if mol is None:
    raise ValueError("Could not parse SMILES")

mol = Chem.AddHs(mol)

# Try normal ETKDG first
params = AllChem.ETKDGv3()
params.randomSeed = 42

result = AllChem.EmbedMolecule(mol, params)

# Fallback for difficult molecules
if result != 0:
    for seed in range(20):
        params = AllChem.ETKDGv3()
        params.randomSeed = seed
        params.useRandomCoords = True

        result = AllChem.EmbedMolecule(mol, params)

        if result == 0:
            break

if result != 0:
    raise RuntimeError("Could not generate 3D conformer")

# Optimize only if MMFF parameters exist
if AllChem.MMFFHasAllMoleculeParams(mol):
    AllChem.MMFFOptimizeMolecule(mol)

conf = mol.GetConformer()

coords = []

for atom in mol.GetAtoms():
    pos = conf.GetAtomPosition(atom.GetIdx())

    coords.append({
        "Element": atom.GetSymbol(),
        "x": pos.x,
        "y": pos.y,
        "z": pos.z
    })

print(json.dumps(coords))
    
