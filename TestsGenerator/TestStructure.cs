namespace TestsGenerator
{
    public class TestStructure
    {
        public string TestName; // Имя класса для которого сгенерирован файл с тестом
        public string TestCode; // Код тест-файла

        public TestStructure(string name, string content)
        {
            TestName = name + ".cs";
            TestCode = content;
        }
    }
}