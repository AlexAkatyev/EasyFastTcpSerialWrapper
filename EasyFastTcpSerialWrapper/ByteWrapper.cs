/*
 * Сообщения передаются потоком.
 * Для примера ranger == 0x20, flag == 0x01.
 * Начало и конец любого сообщения выделяется разделителем 0x20 0x20 0x20 0x20 0x20 0x20.
 * Если требуется передать данные, совпадающие с разделителем,
 * то последовательность будет выглядеть  0x20 0x20 0x20 0x20 0x20 0x01 0x20.
 */

using System.Collections.Generic;

namespace EasyFastTcpSerialWrapper;

public class ByteWrapper
{
    private const int DEF_RANGER = 0x20;
    private const int DEF_FLAG = 0x01;
    private const int WRAP_LEN = 6;
    private const int NOT_FOUND = -1;

    public ByteWrapper(byte ranger = DEF_RANGER, byte flag = DEF_FLAG)
    {
        _ranger = ranger;
        _flag = flag;
    }


    public void SendMessage(byte[] message)
    {
        _sendBuffer.AddRange(_getRanger());
        int counterR = 0;
        for (int i = 0; i < message.Length; i++)
        {
            if (message[i] == _ranger)
            {
                counterR++;
            }
            else
            {
                counterR = 0;
            }
            if (counterR == WRAP_LEN)
            {
                _sendBuffer.Add(_flag);
                counterR = 0;
            }
            _sendBuffer.Add(message[i]);
        }
        _sendBuffer.AddRange(_getRanger());
    }


    public byte[] GetDataToSend()
    {
        byte[] result = _sendBuffer.ToArray();
        _sendBuffer.Clear();
        return result;
    }


    public void ReceiveData(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            _receiveBuffer.Add(data[i]);
        }
        _decodeReceiveData();
    }


    public List<List<byte>> GetReceivedMessages()
    {
        List<List<byte>> result = [];
        for (int i = 0; i < _receiveMessages.Count; i++)
        {
            byte[] message = new byte[_receiveMessages[i].Count];
            _receiveMessages[i].CopyTo(message);
            result.Add([.. message]);
        }
        _receiveMessages.Clear();
        return result;
    }

    
    private List<byte> _getRanger()
    {
        List<byte> result = [];
        for (int i = 0; i < WRAP_LEN; i++)
        {
            result.Add(_ranger);
        }
        return result;
    }


    private void _decodeReceiveData()
    {
        List<byte> ranger = _getRanger();
        while (_receiveBuffer.Count >= WRAP_LEN * 2 + 1)
        {
            int lh = _indexOf(_receiveBuffer, ranger, 0);
            if (lh == NOT_FOUND)
            {
                break;
            }
            int rh = _indexOf(_receiveBuffer, ranger, lh + WRAP_LEN);
            if (rh == NOT_FOUND)
            {
                break;
            }

            List<byte> message = new List<byte>();
            for (int index = lh + WRAP_LEN; index < rh; index++)
            {
                message.Add(_receiveBuffer[index]);
            }
            if (message.Count > 0)
            {
                _receiveMessages.Add(message);
            }
            _receiveBuffer.RemoveRange(0, rh + WRAP_LEN);
        }
    }


    private int _indexOf(List<byte> source, List<byte> item, int beginPosition)
    {
        int result = NOT_FOUND;
        if (beginPosition > source.Count - item.Count)
        {
            return result;
        }
        for (int index = source.IndexOf(item[0], beginPosition);
            index != NOT_FOUND && index <= source.Count - item.Count;
            index = source.IndexOf(item[0], index + 1))
        {
            bool find = true;
            for (int i = 0; i < item.Count; i++)
            {
                find &= (source[index + i] == item[i]);
            }
            if (find)
            {
                result = index;
                break;
            }
        }
        return result;
    }


    private byte _ranger;
    private byte _flag;
    private readonly List<byte> _sendBuffer = [];
    private readonly List<byte> _receiveBuffer = [];
    private readonly List<List<byte>> _receiveMessages = [];
}
