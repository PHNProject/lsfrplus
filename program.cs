// using System;
using System.IO;
using System.Security.Cryptography;
using System.Management;

class Program
{
    static void Main()
    {
        string language = SelectLanguage();
        string message = language == "rus"
            ? "Данная программа, вместе с алгоритмом шифрования, разработана студентом колледжа ITHub группы 4ИБ2.21 Морозовым Денисом Алексеевичем в рамках дипломного проекта."
            : "This program, together with the encryption algorithm, was developed by a student of the ITHub college, group 4IS2.21, Denis Alekseevich Morozov, as part of his diploma project.";

        Console.WriteLine(message);
        string action = GetAction(language);
        string filePath = GetFilePath(language);

        byte[] salt = GenerateSaltFromDeviceSerial();
        byte[] key = GenerateKeyFromLSFR(salt);

        if (action == "encrypt")
            EncryptFile(filePath, key);
        else if (action == "decrypt")
            DecryptFile(filePath, key);

        Console.WriteLine(language == "rus" ? "Готово!" : "Done!");
        Console.WriteLine(language == "rus" ? "Нажмите любую клавишу для продолжения..." : "Press any key to continue...");
        Console.ReadKey();
    }

    static string SelectLanguage()
    {
        while (true)
        {
            Console.WriteLine("Выберите язык / Select language (rus/eng):");
            string lang = Console.ReadLine()?.Trim().ToLower();
            if (lang == "rus" || lang == "eng") return lang;
            Console.WriteLine("Error");
        }
    }

    static string GetAction(string language)
    {
        while (true)
        {
            Console.WriteLine(language == "rus" ? "Файл нужно зашифровать или расшифровать? (encrypt/decrypt):" : "File needs to be encrypted or decrypted? (encrypt/decrypt):");
            string action = Console.ReadLine()?.Trim().ToLower();
            if (action == "encrypt" || action == "decrypt") return action;
            Console.WriteLine("Error");
        }
    }

    static string GetFilePath(string language)
    {
        Console.WriteLine(language == "rus" ? "Введите полный путь к файлу (Пример: C:\\Users\\user\\Desktop\\file.docx(.enc)):" : "Enter the full path to the file (Example: C:\\Users\\user\\Desktop\\file.docx(.enc)):");
        return Console.ReadLine();
    }

    static byte[] GenerateSaltFromDeviceSerial()
    {
        string serial = GetDeviceSerialNumber();
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(serial));
        }
    }

    static string GetDeviceSerialNumber()
    {
        string serial = "DefaultSerial";
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (ManagementObject obj in searcher.Get())
            {
                serial = obj["SerialNumber"].ToString();
                break;
            }
        }
        catch
        {
            serial = "FallbackSerial";
        }
        return serial;
    }

    static byte[] GenerateKeyFromLSFR(byte[] salt)
    {
        int[] lfsr1 = { 0x1A, 0x2C, 0x3D, 0x5F, 0x8E };
        int[] lfsr2 = { 0x2B, 0x4F, 0x7D, 0x1C, 0x3A };

        byte[] key = new byte[32];
        for (int i = 0; i < key.Length; i++)
        {
            int bit1 = (lfsr1[0] ^ lfsr1[2] ^ lfsr1[4]) & 1;
            int bit2 = (lfsr2[1] ^ lfsr2[3]) & 1;

            for (int j = lfsr1.Length - 1; j > 0; j--) lfsr1[j] = lfsr1[j - 1];
            for (int j = lfsr2.Length - 1; j > 0; j--) lfsr2[j] = lfsr2[j - 1];

            lfsr1[0] = bit1;
            lfsr2[0] = bit2;

            key[i] = (byte)(salt[i % salt.Length] ^ (bit1 | bit2 << 1));
        }
        return key;
    }

    static void EncryptFile(string filePath, byte[] key)
    {
        byte[] data = File.ReadAllBytes(filePath);
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = new byte[16];
            using (FileStream fs = new FileStream(filePath + ".enc", FileMode.Create))
            using (CryptoStream cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
        }
    }

    static void DecryptFile(string filePath, byte[] key)
    {
        byte[] data = File.ReadAllBytes(filePath);
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = new byte[16];
            using (FileStream fs = new FileStream(filePath.Replace(".enc", ""), FileMode.Create))
            using (CryptoStream cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
        }
    }
}
