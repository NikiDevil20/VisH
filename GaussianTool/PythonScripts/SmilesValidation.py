import sys
import json
from rdkit import Chem
from rdkit.Chem import AllChem

smiles_string = sys.argv[1]

mol = Chem.MolFromSmiles(smiles_string)

if mol is None:
    print("Error")
else:
    print("valid")