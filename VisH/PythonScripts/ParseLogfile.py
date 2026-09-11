from pathlib import Path
import json
import re
import sys
import traceback

ATOMIC_SYMBOLS = {
    1: "H", 2: "He", 3: "Li", 4: "Be", 5: "B", 6: "C", 7: "N", 8: "O", 9: "F", 10: "Ne",
    11: "Na", 12: "Mg", 13: "Al", 14: "Si", 15: "P", 16: "S", 17: "Cl", 18: "Ar",
    19: "K", 20: "Ca", 21: "Sc", 22: "Ti", 23: "V", 24: "Cr", 25: "Mn", 26: "Fe",
    27: "Co", 28: "Ni", 29: "Cu", 30: "Zn", 31: "Ga", 32: "Ge", 33: "As", 34: "Se",
    35: "Br", 36: "Kr", 37: "Rb", 38: "Sr", 39: "Y", 40: "Zr", 41: "Nb", 42: "Mo",
    43: "Tc", 44: "Ru", 45: "Rh", 46: "Pd", 47: "Ag", 48: "Cd", 49: "In", 50: "Sn",
    51: "Sb", 52: "Te", 53: "I", 54: "Xe", 55: "Cs", 56: "Ba", 57: "La", 58: "Ce",
    59: "Pr", 60: "Nd", 61: "Pm", 62: "Sm", 63: "Eu", 64: "Gd", 65: "Tb", 66: "Dy",
    67: "Ho", 68: "Er", 69: "Tm", 70: "Yb", 71: "Lu", 72: "Hf", 73: "Ta", 74: "W",
    75: "Re", 76: "Os", 77: "Ir", 78: "Pt", 79: "Au", 80: "Hg", 81: "Tl", 82: "Pb",
    83: "Bi", 84: "Po", 85: "At", 86: "Rn", 87: "Fr", 88: "Ra", 89: "Ac", 90: "Th",
    91: "Pa", 92: "U", 93: "Np", 94: "Pu", 95: "Am", 96: "Cm", 97: "Bk", 98: "Cf",
    99: "Es", 100: "Fm", 101: "Md", 102: "No", 103: "Lr", 104: "Rf", 105: "Db", 106: "Sg",
    107: "Bh", 108: "Hs", 109: "Mt", 110: "Ds", 111: "Rg", 112: "Cn", 113: "Nh", 114: "Fl",
    115: "Mc", 116: "Lv", 117: "Ts", 118: "Og"
}

def parse_logfile(logfile_path):
    text = Path(logfile_path).read_text(encoding="utf-8", errors="ignore")
    json_dict = {}

    json_dict["NHomo"] = parse_nhomo(text)
    json_dict["AllFreqs"] = parse_frequencies(text)
    json_dict["MoEnergies"] = parse_mo_energies(text)
    json_dict["ScfEnergies"] = parse_scf_energies(text)
    json_dict["CoordResults"] = parse_coordinates(text)
    json_dict["Version"] = 1

    return json_dict

def parse_nhomo(text):
    matches = re.findall(r"N\s*Imag\s*=\s*(\d+)", text, re.IGNORECASE)
    return int(matches[-1]) if matches else 0

def parse_frequencies(text):
    freqs = []
    for match in re.findall(r"Frequencies --\s+([-\d.\s]+)", text):
        freqs.extend(float(value) for value in match.split())
    return freqs

def parse_scf_energies(text):
    energies = []
    for match in re.findall(r"SCF Done:\s+E\([^)]+\)\s+=\s+(-?\d+\.\d+)", text):
        energies.append(float(match))
    return energies

def parse_mo_energies(text):
    matches = re.findall(r"Alpha\s+occ\.\s+eigenvalues\s+--\s+([-\d.\s]+)", text)
    if not matches:
        return []
    values = []
    for line in matches:
        values.extend(float(value) for value in line.split())
    return values

def parse_coordinates(text):
    blocks = re.findall(
        r"Standard orientation:\s+.*?-+\s+Center\s+Atomic\s+Atomic\s+Coordinates \(Angstroms\)\s+-+\s+(.*?)\s+-+",
        text,
        re.DOTALL
    )
    if not blocks:
        return ""

    last_block = blocks[-1]
    molecule = []
    for line in last_block.splitlines():
        parts = line.split()
        if len(parts) < 6:
            continue
        atomnumber = int(parts[1])
        atomsymbol = ATOMIC_SYMBOLS.get(atomnumber, "X")
        x, y, z = parts[3], parts[4], parts[5]
        molecule.append(f"{atomsymbol} {x} {y} {z}")

    return "\n".join(molecule)

if __name__ == "__main__":
    logfile = sys.argv[1]
    directory = Path(sys.argv[2])
    log_path = directory / "parse_logfile_debug.txt"

    try:
        with open(log_path, "a", encoding="utf-8") as debug:
            debug.write(f"logfile={logfile}\n")
            debug.write(f"directory={directory}\n")
            debug.write("starting parse\n")

        json_dict = parse_logfile(logfile)

        result_path = directory / "result.json"
        with open(result_path, "w", encoding="utf-8") as f:
            json.dump(json_dict, f, indent=2)
            f.flush()

        with open(log_path, "a", encoding="utf-8") as debug:
            debug.write(f"result.json written to {result_path}\n")
            debug.write(f"exists={result_path.exists()}\n")
    except Exception:
        with open(log_path, "a", encoding="utf-8") as debug:
            debug.write(traceback.format_exc())
        raise
