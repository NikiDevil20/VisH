// namespace VisH.Model.PostRun;
//
// public class Orbitals : CalcResults
// {
//     public double HOMO { get; set; }
//     public double LUMO { get; set; }
//     public double Gap { get; set; }
//     
//     public Orbitals(double[] energies, int nHomo)
//     {
//         HOMO = energies[nHomo];
//         LUMO = energies[nHomo+1];
//         Gap = LUMO - HOMO;
//     }
//     
//     public Dictionary<string, string> ToDictionary()
//     {
//         return new Dictionary<string, string>()
//         {
//             { "HOMO / eV", HOMO.ToString("F2") },
//             { "LUMO / eV", LUMO.ToString("F2") },
//             { "Gap / eV", Gap.ToString("F2") },
//             { "HOMO / Hartree", ElectronVoltToHartree(HOMO).ToString("F2") },
//             { "LUMO / Hartree", ElectronVoltToHartree(LUMO).ToString("F2") },
//             { "Gap / Hartree", ElectronVoltToHartree(Gap).ToString("F2") }
//         };
//     }
// }
