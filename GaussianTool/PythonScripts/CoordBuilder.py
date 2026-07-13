import sys
import json
from rdkit import Chem
from rdkit.Chem import AllChem

coords = []

smiles_string = sys.argv[1]

mol = Chem.MolFromSmiles(smiles_string)

mol = Chem.AddHs(mol)

AllChem.EmbedMolecule(mol)

AllChem.MMFFOptimizeMolecule(mol)

conf = mol.GetConformer()

for atom in mol.GetAtoms():
    pos = conf.GetAtomPosition(atom.GetIdx())
    
    coords.append({
        "Element": atom.GetSymbol(),
        "x": pos.x,
        "y": pos.y,
        "z": pos.z
    })
    
print(json.dumps(coords))
    
