using System;
using System.Text;

class Program
{
    // Единственная глобальная переменная — сама "оперативная память"
    private static readonly byte[] Memory = new byte[1024];

    private const int HeaderSize = 3;   // статус(1) + размер(2)
    private const byte Free = 0;
    private const byte Used = 1;

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        InitMemory();

        while (true)
        {
            Console.WriteLine("\n=== Меню ===");
            Console.WriteLine("1. Выделить память");
            Console.WriteLine("2. Освободить память");
            Console.WriteLine("3. Состояние памяти");
            Console.WriteLine("0. Выход");
            Console.Write("Выбор: ");

            switch (Console.ReadLine())
            {
                case "1":
                    Console.Write("Размер (байт): ");
                    if (int.TryParse(Console.ReadLine(), out int size))
                    {
                        int addr = Allocate(size);
                        if (addr >= 0)
                            Console.WriteLine($"OK: выделено по адресу {addr}");
                    }
                    break;
                case "2":
                    Console.Write("Начальный адрес (полученный при выделении): ");
                    if (int.TryParse(Console.ReadLine(), out int address))
                        FreeBlock(address);
                    break;
                case "3":
                    PrintMemoryState();
                    break;
                case "0":
                    return;
            }
        }
    }

    // ---------- инициализация ----------
    static void InitMemory()
    {
        Memory[0] = Free;
        WriteUShort(1, (ushort)(Memory.Length - HeaderSize));
    }

    // ---------- низкоуровневые помощники ----------
    static void WriteUShort(int off, ushort v)
    {
        Memory[off] = (byte)(v >> 8);
        Memory[off + 1] = (byte)(v & 0xFF);
    }
    static ushort ReadUShort(int off) => (ushort)((Memory[off] << 8) | Memory[off + 1]);

    static byte GetStatus(int h) => Memory[h];
    static void SetStatus(int h, byte s) => Memory[h] = s;
    static ushort GetSize(int h) => ReadUShort(h + 1);
    static void SetSize(int h, ushort sz) => WriteUShort(h + 1, sz);
    static int NextHeader(int h) => h + HeaderSize + GetSize(h);

    // ---------- а) выделение (Worst Fit) ----------
    static int Allocate(int size)
    {
        if (size <= 0) { Console.WriteLine("Размер должен быть > 0"); return -1; }

        int bestOffset = -1;
        int maxFreeSize = -1;

        int off = 0;
        while (off < Memory.Length)
        {
            if (GetStatus(off) == Free && GetSize(off) >= size)
            {
                if (GetSize(off) > maxFreeSize)
                {
                    maxFreeSize = GetSize(off);
                    bestOffset = off;
                }
            }
            off = NextHeader(off);
        }

        if (bestOffset != -1)
        {
            int remaining = GetSize(bestOffset) - size;

            if (remaining >= HeaderSize + 1)
            {
                SetStatus(bestOffset, Used);
                SetSize(bestOffset, (ushort)size);

                int newH = bestOffset + HeaderSize + size;
                SetStatus(newH, Free);
                SetSize(newH, (ushort)(remaining - HeaderSize));
            }
            else
            {
                SetStatus(bestOffset, Used);
            }

            return bestOffset + HeaderSize;
        }

        Console.WriteLine($"Недостаточно памяти: не нашлось свободного блока на {size} байт");
        return -1;
    }

    // ---------- освобождение ----------
    static bool FreeBlock(int dataAddress)
    {
        int h = dataAddress - HeaderSize;

        if (h < 0 || h >= Memory.Length)
        {
            Console.WriteLine("Неверный адрес");
            return false;
        }
        if (GetStatus(h) != Used)
        {
            Console.WriteLine($"По адресу {dataAddress} нет занятого блока");
            return false;
        }

        SetStatus(h, Free);
        CoalesceAll();
        Console.WriteLine($"Освобождён блок по адресу {dataAddress}");
        return true;
    }

    // Соединение соседних пустых блоков
    static void CoalesceAll()
    {
        int cur = 0;
        while (true)
        {
            int next = NextHeader(cur);
            if (next >= Memory.Length) break;

            if (GetStatus(cur) == Free && GetStatus(next) == Free)
            {
                SetSize(cur, (ushort)(GetSize(cur) + HeaderSize + GetSize(next)));
            }
            else
            {
                cur = next;
            }
        }
    }

    // ---------- в) информация ----------
    static void PrintMemoryState()
    {
        Console.WriteLine("\n=== Состояние памяти ===");
        Console.WriteLine(" Адрес | Статус   | Размер");
        Console.WriteLine("-------+----------+-------");

        int freeCount = 0, usedCount = 0;
        int freeBytes = 0, usedBytes = 0;

        int off = 0;
        while (off < Memory.Length)
        {
            byte st = GetStatus(off);
            ushort sz = GetSize(off);

            string label = st == Free ? "свободен" : "занят   ";
            if (st == Free) { freeCount++; freeBytes += sz; }
            else { usedCount++; usedBytes += sz; }

            Console.WriteLine($"{off + HeaderSize,6} | {label} | {sz,5}");
            off = NextHeader(off);
        }

        Console.WriteLine($"Свободных блоков: {freeCount}, занятых: {usedCount}");
        Console.WriteLine($"Свободно (данные): {freeBytes} байт");
        Console.WriteLine($"Занято   (данные): {usedBytes} байт");
    }
}