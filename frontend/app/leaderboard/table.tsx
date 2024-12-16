"use client";
import { Table, TableHeader, TableColumn, TableBody, TableRow, TableCell } from "@nextui-org/table";
import { Pagination } from "@nextui-org/pagination";
import { Skeleton } from "@nextui-org/skeleton";
import { UserModel } from "@/api/types";
import { User } from "@/components/user";
import { ApiBase } from "@/api/address";
import { Input } from "@nextui-org/input";
import { Spacer } from "@nextui-org/spacer";
import { FC, useEffect, useState } from "react";
import { Select, SelectItem } from "@nextui-org/select";
import useSWR from "swr";

const fetcher = (url : any) => fetch(url).then(r => r.json()).catch(error => console.log("Leaderboard fetch error: " + error));;
type Props = { countries: string[] | undefined };

export const LeaderboardTable: FC<Props> = (props) => {
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState("");
    const [country, setCountry] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState("");
    
    const countries = ["-"].concat(props.countries ? props.countries : []);

    useEffect(() => {
      const timeoutId = setTimeout(() => {
        setDebouncedSearch(search);
      }, 500);
      return () => clearTimeout(timeoutId);
    }, [search, 500]);

    const handleCountrySelectionChange = (e:any) => {
      setCountry(e.target.value);
    };

    let address = `${ApiBase}/players?page=${page}`;
    if (debouncedSearch != "") {
      address += `&search=${debouncedSearch}`
    }
    if (country != "" && country != "-") {
      address += `&countryCode=${country}`
    }
    const { data, error } = useSWR(address, fetcher)

    const placeholderRows = Array.from(Array(25), (_, i) => (<TableRow key={i}>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
      <TableCell><Skeleton className="rounded-md"><div className="h-6"></div></Skeleton></TableCell>
    </TableRow>))

    if (error) return <div>Failed to load</div>
    if (!data) 
    return (
    <>
      <div className="w-full flex justify-end">
        <Input
            classNames={{
              base: "mx-4 max-w-48 h-10",
              mainWrapper: "h-full",
              input: "text-small",
              inputWrapper: "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
            }}
            placeholder="Search player..."
            size="sm"
            type="search"
            value={debouncedSearch}
            disabled
          />
      </div>
      <Spacer y={2} />
      <Table isCompact fullWidth={true} bottomContent={
        <div className="flex w-full justify-center"><Pagination isCompact showControls isDisabled showShadow color="secondary" total={0} /></div>
        }>
        <TableHeader>
          <TableColumn width={55} align="start">{""}</TableColumn>
          <TableColumn className="text-ellipsis">Username</TableColumn>
          <TableColumn align="center">Accuracy</TableColumn>
          <TableColumn align="center">PP</TableColumn>
        </TableHeader>
        <TableBody>{placeholderRows}</TableBody>
      </Table>
    </>)

    const pages = Math.max(1,Math.ceil(data.total / 50));
    const offset = (page-1) * 50;
    let players = data.players;

    return (
    <>
      
      <div className="w-full flex gap-2">
        {props.countries ? <div className="w-full flex justify-start">
          <Select className="md:ml-4 max-w-52" 
            size="sm" 
            label="Country" 
            placeholder="Select a country" 
            selectedKeys={[country]} 
            onChange={handleCountrySelectionChange}>
            {countries.map((country) => (
              <SelectItem key={country}>{country}</SelectItem>
            ))}
          </Select>
        </div> : <></>}
        <Input
            classNames={{
              base: "md:mr-4 max-w-52 justify-end",
              mainWrapper: "h-full",
              input: "text-small",
              inputWrapper: "h-full font-normal text-default-500 bg-default-400/20 dark:bg-default-500/20",
            }}
            placeholder="Search player..."
            size="sm"
            type="search"
            value={search}
            onValueChange={setSearch}
          />
      </div>
      <Spacer y={2} />
      <Table isStriped isCompact fullWidth={true}
          bottomContent={
          <div className="flex w-full justify-center">
            <Pagination
              isCompact
              showControls
              showShadow
              color="primary"
              page={page}
              total={pages}
              onChange={(page) => {setPage(page)}}
            />
          </div>
        }>
        <TableHeader>
          <TableColumn width={55} align="start">{""}</TableColumn>
          <TableColumn >Username</TableColumn>
          <TableColumn align="center">Accuracy</TableColumn>
          <TableColumn align="center">PP</TableColumn>
        </TableHeader>
        <TableBody>
          {players.map((row: UserModel, index: number) => (
            <TableRow key={row.id}>
              <TableCell className="text-default-500">#{offset+index+1}</TableCell>
              <TableCell><User user={row}/></TableCell>
              <TableCell><p className="text-default-500">{row.totalAccuracy === null ? "-" : row.totalAccuracy?.toFixed(2)}%</p></TableCell>
              <TableCell className="text-primary-400 font-semibold">{row.totalPp === null ? "-" : row.totalPp?.toFixed(0)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </>
  );
}
