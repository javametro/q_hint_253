using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusGetter
{
    //同一LAN上のデバイス情報
    //同一LAN上のデバイス一覧情報を取得する。
    //（IPアドレス、MACアドレス、TTL、デバイス種別）

    //・全IPアドレスにPingコマンドを送った後に、ARPテーブル取得することで情報を取得する。TTLはPingの応答パケットを参照する。デバイス種別はTTLを見て判断して、PC（128）かそれ以外かを判別する。ソース提供有。

    internal class LocalAreaNetworkDeviceInfo
    {
    }
}
