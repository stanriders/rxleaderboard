"use client";
import { Card, CardBody } from "@nextui-org/card";
import { FC } from "react";
import {
  CartesianGrid,
  Area,
  AreaChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { PlaycountPerMonth } from "@/api/types";

type Props = {
  playcountsPerMonth: PlaycountPerMonth[] | null | undefined;
  showDate?: boolean;
  leftMargin?: number | null;
};

export const PlaycountChart: FC<Props> = (props) => {
  if (!props.playcountsPerMonth) return <></>;

  const dayFormat = props.showDate ? "numeric" : undefined;
  var data = props.playcountsPerMonth.map((val) => {
    return {
      date: new Date(val.date ?? "").toLocaleString("default", {
        day: dayFormat,
        month: "short",
        year: "numeric",
      }),
      playcount: val.playcount,
    };
  });

  const CustomTooltip = ({ active, payload, label }: any) => {
    if (active && payload && payload.length) {
      return (
        <Card shadow="sm">
          <CardBody>
            <p className="text-xs text-default-600">{label}</p>
            <p className="text-sm text-primary-500">{payload[0].value} plays</p>
          </CardBody>
        </Card>
      );
    }

    return null;
  };

  return (
    <ResponsiveContainer>
      <AreaChart
        data={data}
        margin={{ right: 35, top: 20, left: props.leftMargin ?? 0 }}
      >
        <Area dataKey="playcount" fill="#DBB33F" stroke="#DBB33F" type="bump" />
        <XAxis className="text-xs" dataKey="date" />
        <YAxis className="text-xs" width={40} />
        <Tooltip content={<CustomTooltip />} />
        <CartesianGrid strokeDasharray="3 3" strokeOpacity="0.1" />
      </AreaChart>
    </ResponsiveContainer>
  );
};
