// using System.Windows;
//
// namespace VisH.Model.Parameters;
//
// public class KeywordSelector
// {
//     public static string[][]? GetKeywordsAndLink(string calcType, States? state)
//     {
//         if (state == null)
//         {
//             MessageBox.Show("Invalid state", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//             return null;
//         }
//
//         switch (calcType)
//         {
//             case "OPT":
//                 var keywordsAndLink = OptKeywords(state);
//                 return keywordsAndLink;
//             case "TD":
//                 return TdKeywords(state);
//             default:
//                 MessageBox.Show("Invalid calculation type", "Error", MessageBoxButton.OK, 
//                     MessageBoxImage.Error);
//                 return null;
//         }
//         
//         
//         
//     }
//
//     private static string[][] OptKeywords(States state)
//     {
//         List<string> keywords = new List<string>();
//         List<string> linkKeywords = new List<string>();
//         
//         keywords.Add("opt");
//         keywords.Add("scf=xqc");
//
//         if (state.ExcitedN == "0")
//         {
//             keywords.Add("pop=full");
//             keywords.Add("GFInput");
//             
//             linkKeywords.Add("freq");
//             linkKeywords.Add("geom=AllCheck");
//             linkKeywords.Add("guess=TCheck");
//             linkKeywords.Add("SCRF=Check");
//             linkKeywords.Add("GenChk");
//             linkKeywords.Add("Test");
//         }
//         else
//         {
//             keywords.Add("freq");
//             switch (state.Multiplicity)
//             {
//                 case "1":
//                     keywords.Add("TD=(singlet,Nstates=6,root=1)");
//                     break;
//                 case "3":
//                     keywords.Add("TD=(triplet,Nstates=6,root=1)");
//                     break;
//                 default:
//                     MessageBox.Show("Invalid multiplicity", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                     break;
//             }
//         }
//         return [keywords.ToArray(), linkKeywords.ToArray()];
//     }
//
//     private static string[][] TdKeywords(States state)
//     {
//         throw new NotImplementedException();
//     }
// }
