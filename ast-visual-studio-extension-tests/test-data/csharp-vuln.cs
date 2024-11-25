using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ast_visual_studio_extension_tests.test_data

{
internal class csharp_vuln
{
    public void PrintPassword()
    {
        var password = "Aa123456";
        Console.WriteLine(password);
    }
}
}
