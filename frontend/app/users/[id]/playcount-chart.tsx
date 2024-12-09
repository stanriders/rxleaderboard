"use client";
import { PlaycountPerMonth } from "@/api/types";
import { Card, CardBody } from "@nextui-org/card";
import { FC } from "react";
import { CartesianGrid, Area, AreaChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";

type Props = { playcountsPerMonth: PlaycountPerMonth[] | null | undefined };

export const PlaycountChart: FC<Props> = (props) => {
    
  if (!props.playcountsPerMonth)
    return <></>;

  var data = props.playcountsPerMonth.map((val)=> {
    return {date: new Date(val.date ?? "").toLocaleString('default', { month: 'short', year: 'numeric' }), playcount: val.playcount};
  })

  const CustomTooltip = ({ active, payload, label } : any) => {
    if (active && payload && payload.length) {
      return (
        <Card shadow="sm">
            <CardBody><p className="text-xs text-default-600">{label}</p>
            <p className="text-sm text-primary-500">{payload[0].value} plays</p></CardBody>
        </Card>
      );
    }
    return null;
  };

  return (
    <ResponsiveContainer>
        <AreaChart data={data} margin={{ right: 35, top: 20, left: 0 }}>
            <Area type="bump" dataKey="playcount" stroke="#DBB33F" fill="#DBB33F" />
            <XAxis dataKey="date" className="text-xs"/>
            <YAxis className="text-xs" width={40}/>
            <Tooltip content={<CustomTooltip />}/>
            <CartesianGrid strokeDasharray="3 3" strokeOpacity="0.1" />
        </AreaChart>
    </ResponsiveContainer>
  );
}

