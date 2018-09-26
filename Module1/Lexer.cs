using System.Collections.Generic;


public class Option<T>
{
    public readonly bool has_value;
    public readonly T value;

    public Option()
    {
        has_value = false;
    }
    public Option(T v)
    {
        has_value = true;
        value = v;
    }

    public static implicit operator Option<T>(T v)
    {
        return new Option<T>(v);
    }
}

public class ValueList<T> : List<T>
{
    public override bool Equals(object other)
    {
        var as_list = other as ValueList<T>;
        if (as_list == null)
            return false;
        return System.Linq.Enumerable.SequenceEqual<T>(this, as_list);
    }
}

public class LexerException : System.Exception
{
    public LexerException(string msg)
        : base(msg)
    {
    }

}

public class Lexer<T>
{
    public T result;

    protected int position;
    protected char currentCh;       // очередной считанный символ
    protected int currentCharValue; // целое значение очередного считанного символа
    protected System.IO.StringReader inputReader;
    protected string inputString;

   

    public Lexer(string input)
    {
        inputReader = new System.IO.StringReader(input);
        inputString = input;
    }

    public void Error()
    {
        System.Text.StringBuilder o = new System.Text.StringBuilder();
        o.Append('\n' + inputString + '\n');
        o.Append(new System.String(' ', position - 1) + "^\n");
        o.AppendFormat("Error in symbol {0}", currentCh);
        throw new LexerException(o.ToString());
    }

    protected void NextCh()
    {
        this.currentCharValue = this.inputReader.Read();
        this.currentCh = (char)currentCharValue;
        this.position += 1;
    }

    public virtual void Parse()
    {

    }

}

// целые числа
public class IntLexer : Lexer<int>
{

    protected System.Text.StringBuilder intString;

    public IntLexer(string input)
        : base(input)
    {
        intString = new System.Text.StringBuilder();
    }

    public override void Parse()
    {
        NextCh();
        if (currentCh == '+' || currentCh == '-')
        {
            intString.Append(currentCh);
            NextCh();
        }

        if (char.IsDigit(currentCh))
        {
            intString.Append(currentCh);
            NextCh();
        }
        else
        {
            Error();
        }

        while (char.IsDigit(currentCh))
        {
            intString.Append(currentCh);
            NextCh();
        }


        if (currentCharValue != -1) // StringReader вернет -1 в конце строки
        {
            Error();
        }

        result = int.Parse(intString.ToString());

    }
}

// идентификатор
public class IDLexer : Lexer<string>
{

    protected System.Text.StringBuilder idString;

    public IDLexer(string input)
        : base(input)
    {
        idString = new System.Text.StringBuilder();
    }

    public override void Parse()
    {
        NextCh();

        if (char.IsLetter(currentCh))
        {
            idString.Append(currentCh);
            NextCh();
        }
        else
        {
            Error();
        }

        while (char.IsLetterOrDigit(currentCh))
        {
            idString.Append(currentCh);
            NextCh();
        }


        if (currentCharValue != -1) // StringReader вернет -1 в конце строки
        {
            Error();
        }

        result = idString.ToString();

    }
}

// целые со знаком, начинающиеся не с нуля
public class NZIntLexer : Lexer<int>
{

    protected System.Text.StringBuilder intString;

    public NZIntLexer(string input)
        : base(input)
    {
        intString = new System.Text.StringBuilder();
    }

    public override void Parse()
    {
        NextCh();
        if (currentCh == '+' || currentCh == '-')
        {
            intString.Append(currentCh);
            NextCh();
        }
        else
        {
            Error();
        }

        if (char.IsDigit(currentCh) && currentCh != '0')
        {
            intString.Append(currentCh);
            NextCh();
        }
        else
        {
            Error();
        }

        while (char.IsDigit(currentCh))
        {
            intString.Append(currentCh);
            NextCh();
        }


        if (currentCharValue != -1) // StringReader вернет -1 в конце строки
        {
            Error();
        }

        result = int.Parse(intString.ToString());

    }
}

// чередующиеся буквы и цифры, начиная с буквы
public class AlterLexer : Lexer<string>
{

    protected System.Text.StringBuilder idString;

    public AlterLexer(string input)
        : base(input)
    {
        idString = new System.Text.StringBuilder();
    }

    public override void Parse()
    {
        NextCh();

        if (char.IsLetter(currentCh))
        {
            idString.Append(currentCh);
            NextCh();
        }
        else
        {
            Error();
        }

        bool should_be_digit = true;

        while (should_be_digit && char.IsDigit(currentCh)
            || !should_be_digit && char.IsLetter(currentCh))
        {
            idString.Append(currentCh);
            NextCh();
            should_be_digit = !should_be_digit;
        }


        if (currentCharValue != -1) // StringReader вернет -1 в конце строки
        {
            Error();
        }

        result = idString.ToString();

    }
}

// список букв, разделённый "," или ";"
public class ListLexer : Lexer<ValueList<char>>
{


    public ListLexer(string input)
        : base(input)
    {
        result = new ValueList<char>();
    }

    public override void Parse()
    {
        NextCh();

        if (char.IsLetter(currentCh))
        {
            result.Add(currentCh);
            NextCh();
        }
        else
        {
            Error();
        }

        bool should_be_delimeter = true;

        while (should_be_delimeter && (currentCh == ',' || currentCh == ';')
            || !should_be_delimeter && char.IsLetter(currentCh) )
        {
            if(!should_be_delimeter)
                result.Add(currentCh);

            NextCh();
            should_be_delimeter = !should_be_delimeter;
        }


        if (currentCharValue != -1 || !should_be_delimeter) // StringReader вернет -1 в конце строки
        {
            Error();
        }

    }
}

public class Program
{
    public static void Test<T>(System.Type t, string[] inputs, Option<T>[] results)
    {
        if (inputs.Length != results.Length)
        {
            throw new System.ApplicationException("inputs and results has differernt lengths");
        }

        for (int i = 0; i < inputs.Length; ++i)
        {
            bool passed = false;
            Lexer<T> L = (Lexer<T>)System.Activator.CreateInstance(t,(inputs[i]));
            Option<T> res = results[i];
            if (res.has_value)
            {
                try
                {
                    L.Parse();

                    if (EqualityComparer<T>.Default.Equals(L.result, res.value))
                        {
                            passed = true;
                        }
                }
                catch (LexerException)
                {
                    passed = false;
                }
            }
            else
            {
                try
                {
                    L.Parse();
                }
                catch (LexerException)
                {
                    passed = true;
                }
            }

            if (!passed)
            {
                throw new System.ApplicationException(string.Format("test {0} failed in {1}", i, t));
            }
        }
    }

    public static void Main()
    {
        Test<int>(
            typeof(IntLexer),
            new string[] { "154216", "+45", "-78", "fg", "", "5" },
            new Option<int>[] { 154216, 45, -78, new Option<int>(), new Option<int>(), 5 }
            );

        Test<string>(
            typeof(IDLexer),
            new string[] { "154216", "+45", "-78", "fg", "", "5", "g14", "tiger", "j", "j34ggh54dfGFD3" },
            new Option<string>[] { new Option<string>(), new Option<string>(), new Option<string>(),
                "fg", new Option<string>(), new Option<string>(), "g14", "tiger", "j", "j34ggh54dfGFD3" }
            );

        Test<int>(
            typeof(NZIntLexer),
            new string[] { "154216", "+45", "-78", "fg", "", "5", "0998", "+055" },
            new Option<int>[] { new Option<int>(), 45, -78, new Option<int>(), new Option<int>(),
                new Option<int>(), new Option<int>(), new Option<int>() }
            );

        Test<string>(
            typeof(AlterLexer),
            new string[] { "154216", "+45", "-78", "f6", "", "g", "g5h6g4g3n8h5", "4g5h6g4g3n8h5", "dfg" },
            new Option<string>[] { new Option<string>(), new Option<string>(), new Option<string>(),
                "f6", new Option<string>(), "g", "g5h6g4g3n8h5", new Option<string>(), new Option<string>() }
            );

        Test<ValueList<char>>(
            typeof(ListLexer),
            new string[] { "a,b;c,d;e,f,g,h", "a", "", "a,", "123", "dfg" },
            new Option<ValueList<char>>[] { new ValueList<char> { 'a','b','c','d','e','f','g','h'},
                new ValueList<char> { 'a'}, new Option<ValueList<char>>(), new Option<ValueList<char>>(),
                new Option<ValueList<char>>(), new Option<ValueList<char>>() }
            );

    }
}
