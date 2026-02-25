using Game.Tool.Tutorial.Proto;
using Google.Protobuf;

namespace Game.Tool.Tutorial
{
    public class TutorialSerializer
    {
        public static void DoSerialize()
        {
            Person john = new Person
            {
                Id = 1234,
                Name = "John Doe",
                Email = "jdoe@example.com",
                Phones = { new Person.Types.PhoneNumber { Number = "555-4321", Type = Person.Types.PhoneType.Home } }
            };

            using (var output = File.Create("john.dat"))
            {
                john.WriteTo(output);
            }
        }

        public static void DoParse()
        {
            Person john;
            using (var input = File.OpenRead("john.dat"))
            {
                john = Person.Parser.ParseFrom(input);
            }
            Console.WriteLine("Person: " + john.ToString());
            Console.WriteLine("PrintMessage: ");
            PrintMessage(john);
        }

        public static void PrintMessage(IMessage message)
        {
            var descriptor = message.Descriptor;
            foreach (var field in descriptor.Fields.InDeclarationOrder())
            {
                Console.WriteLine(
                    "Field {0} ({1}): {2}",
                    field.FieldNumber,
                    field.Name,
                    field.Accessor.GetValue(message));
            }
        }


        public static void Main()
        {
            DoSerialize();
            DoParse();
        }
    }
}
