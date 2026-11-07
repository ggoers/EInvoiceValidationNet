using System;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace EInvoiceValidationNet.Validation.SchematronValidation.SVRLParsing
{
    internal static class SvrlParser
    {
        public static SvrlSchematronOutput Parse(XDocument svrlDocument)
        {
            if (svrlDocument is null)
            {
                throw new ArgumentNullException(nameof(svrlDocument));
            }

            var serializer = new XmlSerializer(typeof(SvrlSchematronOutput));
            using (var reader = svrlDocument.CreateReader())
            {
                return (SvrlSchematronOutput)serializer.Deserialize(reader);
            }
        }
    }
}
