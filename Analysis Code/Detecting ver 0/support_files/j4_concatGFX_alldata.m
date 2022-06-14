% j4_concatGFX_alldata


% Here we load all data from previous analyses, preparing to save before
% plots next round of jobs.

%%%%%% QUEST DETECT version %%%%%%
%Mac:
datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
%PC:
% datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';


cd([datadir filesep 'ProcessedData'])

pfols = dir([pwd filesep '*summary_data.mat']);
nsubs= length(pfols);
%show ppant numbers:
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

%% concat data:
%preallocate storage:

pidx1=ceil(linspace(1,100,11)); % length n-1
% pidx2= ceil(linspace(1,200,14));% length n-1 (was 13)
pidx2= ceil(linspace(1,200,21));% 

gaittypes = {'single gait' , 'double gait'};
%

[dataINrespPos,dataINtargPos]=deal([]);
sliding_cntrpoints=[]; % x vec for sliding window.
GFX_headY=[];
GFX_TargPosData=[];
GFX_RespPosData=[];
subjIDs={};

for ippant =1:nsubs
    cd([datadir filesep 'ProcessedData'])    %%load data from import job.
    load(pfols(ippant).name, ...
        'Ppant_gaitData', 'Ppant_gaitData_doubGC','PFX_headY', 'PFX_headY_doubleGC', 'subjID');
    
    subjIDs{ippant} = subjID;
    
    disp(['concatenating subject ' subjID]);
    
    % first retrieve index for separate gaits (L/Right feet).
    Ltrials= contains(Ppant_gaitData.pkFoot, 'LR');
    Rtrials= contains(Ppant_gaitData.pkFoot, 'RL');
    Ltrials_db= contains(Ppant_gaitData_doubGC.pkFoot, 'LRL');
    Rtrials_db= contains(Ppant_gaitData_doubGC.pkFoot, 'RLR');
    
    
    % mean head pos:
    GFX_headY(ippant).gc = nanmean(PFX_headY);
    GFX_headY(ippant).doubgc = nanmean(PFX_headY_doubleGC);
    
    
    %% also calculate binned / sliding window versions
    % These take the mean over an index range, per gait cycle position
    
    for nGait=1:2
        if nGait==1
            pidx=pidx1; % indices for binning (single or double gc).
            ppantData= Ppant_gaitData;
            useL=Ltrials;
            useR=Rtrials;
            useAll = 1:length(Ltrials);
        else
            pidx=pidx2;
            ppantData= Ppant_gaitData_doubGC;
            useL=Ltrials_db;
            useR=Rtrials_db;
            useAll = 1:length(Ltrials_db);
        end
        
        trialstoIndex ={useL, useR, useAll};
        % split by gait/ foot (L/R)
        for iLR=1:3
            uset= trialstoIndex{iLR};
            
            %% %%%%%%%%%%%%%%%%
            % Step through different data types :
            
            % note that now, we will sample randomly from all
            % positions, based on the number in real data.
            
            % 3 main analysis (for behaviour).
            %- RT relative to target gait position
            %- RT relative to response gait position
            %- Acc relative to target gait position.
            % nb: acc relative to response, is moot, since incorrect responses
            %(False Alarms) were so few.
            %%%%%%%%%%%%%%%%%%
            % for each of these three types, define the search values
            % to shuffle over:
            
            searchPosIDX = {ppantData.targPosPcnt(uset), ...
                ppantData.clickPcnt(uset), ...
                ppantData.targPosPcnt(uset)};
            %and define the DV of interest:
            searchPosDVs = {ppantData.z_targRT(uset), ...
                ppantData.z_clickRT(uset), ...
                ppantData.targDetected(uset)};
            
            for itype=1:3
                %preallocate:
                outgoing = nan(1,pidx(end));
                allpos = searchPosIDX{itype};
                allDVs = searchPosDVs{itype};
                
                
                % for actual gait pos, select reldata
                for ip=1:pidx(end)
                    useb = find(allpos==ip);
                    
                    
                    %% take mean for these  trials (if RT):
                    if itype<=2
                        outgoing(ip) = nanmean(allDVs(useb));
                        
                    elseif itype==3
                        % else compute accuracy.
                        
                        % remove nans from length calcs.
                        tmpResp = allDVs(useb);
                        useResp = tmpResp(~isnan(tmpResp));
                        outgoing(ip)= nansum(useResp) / length(useResp);
                    end
                    
                end
                
                switch itype
                    case 1
                        targOns_RTs = outgoing;
                    case 2
                        respOns_RTs= outgoing;
                    case 3
                        targOns_Acc= outgoing;
                end
            end % itype
            
            
            %%                 %%  note that we can't have response 'accuracy', per pos,
            %                 % as each response recorded was correct.
            %                 %, unless we count the FA as an incorrect response. but
            %                 %they are few.
            %%                   %% mean Acc at each resp onset position:
            respOns_Acc = nan(1,pidx(end));
            allpos = ppantData.clickPcnt(uset); % gait pcnt for response onset.
            allneg=  ppantData.FApcnt(uset); % false alarm pcnt.
            
            %take  acc for all recorded positions:
            for ip=1:pidx(end)
                useb = find(allpos==ip);
                %calculate accuracy, but avoid nans.
                %note that all resps were correct!
                useResp = ones(1,length(useb));
                
                % any FA?
                usen = find(allneg==ip);
                tmpneg = allneg(usen);
                useneg =tmpneg(~isnan(tmpneg));
                
                respOns_Acc(ip) = sum(useResp) / (sum(useResp) + length(useneg));
            end
            
            %% store the histcounts, for target contrast level per pos.
            tPos= ppantData.targPosPcnt(uset); % gait pcnt for target onset.
            
            targContrIDX = ppantData.targContrastIndx(uset);
            targContr = ppantData.targContrast(uset);
            
            [tgcMat]= deal(nan(7,pidx(end)));
            tcontrasts = nan(1,7);
            for  icontr= 1:7
                tindx = find(targContrIDX==icontr);
                tgcMat(icontr,:) = histcounts(tPos(tindx), pidx(end));
                tcontrasts(icontr) = targContr(tindx(end));
            end
            
            %% store histcounts, per target onset pos, and resp onset pos.
            clkPos= ppantData.clickPcnt(uset); % gait pcnt for response onset.
            FAPos = ppantData.FApcnt(uset);
            
            usetypes = {tPos, clkPos, FAPos};
            for itype=1:3
                datain = usetypes{itype};
                % raw hit counts, per gaitsample:
                tmp_counts = histcounts(datain, pidx(end));
                
                %critical step -convert 0 to nans. this avoids averaging
                %problems.
                tmp= tmp_counts; % trgO_counts, / clkOcounts saved below.
                tmp(tmp==0)=NaN;
                tmptoAvg=tmp;
                
                % rename
                if itype==1
                    trgO_counts = tmp_counts;
                    trgOtoAvg = tmptoAvg; % with nans removed.
                elseif itype==2
                    respO_counts = tmp_counts;
                    respOtoAvg = tmptoAvg;
                elseif itype==3
                    FAPos_counts = tmp_counts;
                    FAtoavg = tmptoAvg;
                end
            end
            %% %%%%%%%%%%%%%%%%%%%
            % Step through all data types, performing binning based on
            % length of pidx.
            %%%%%%%%%%%%%%%%%%%
            %trg onset counts
            %resp onset counts
            %acc per trg onset
            %acc per resp onset
            %rt per trg onset
            %rt per resp onset
            usedata= {trgOtoAvg, respOtoAvg, ... % using ver with NaNs removed
                targOns_Acc, respOns_Acc, targOns_RTs, respOns_RTs};
            
            for itype= 1:6
                
                %% for all types, take mean over bin indices.
                out_bin=nan(1,length(pidx)-1);
                datatobin = usedata{itype};
                for ibin=1:length(pidx)-1
                    idx = pidx(ibin):pidx(ibin+1);
                    
                    out_bin(ibin) = nanmean(datatobin(idx));
                    
                end
                
                switch itype
                    case 1
                        trgCounts_bin = out_bin;
                    case 2
                        respCounts_bin = out_bin;
                    case 3
                        targOns_Acc_bin = out_bin;
                    case 4
                        respOns_Acc_bin = out_bin;
                        
                    case 5
                        targOns_RT_bin= out_bin;
                    case 6
                        respOns_RT_bin = out_bin;
                end
                
            end
            %% %%%%%%%%%%%%%%%%%%%%%%%%% now save:
            gaitnames = {'gc_', 'doubgc_'}; % change fieldname based on nGaits:
            
            %add targ data to one matrix, resp data to another:
            %trg contrast info:
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'trgcontr'])= tcontrasts;
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'trgcontrIDXmatrx']) = tgcMat;
            
            % rest of the relevant (behavioural outcomes) data:
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'counts']) = trgO_counts;
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'Acc']) = targOns_Acc;
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'rts']) = targOns_RTs;
            
            
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'binned_counts']) = trgCounts_bin;
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'binned_Acc']) = targOns_Acc_bin;
            GFX_TargPosData(ippant,iLR).([gaitnames{nGait} 'binned_rts']) = targOns_RT_bin;
            
            %also using response position as index:
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'counts']) = respO_counts;
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'FAs']) = FAPos_counts;
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'Acc']) = respOns_Acc;
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'rts']) = respOns_RTs;
            
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'binned_counts']) = respCounts_bin;
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'binned_Acc']) = respOns_Acc_bin;
            GFX_RespPosData(ippant,iLR).([gaitnames{nGait} 'binned_rts']) = respOns_RT_bin;
            
            
        end % iLR
        
    end % nGait
    
    
end % ppant
%% save GFX
cd([datadir filesep 'ProcessedData' filesep 'GFX']);
disp(['Saving GFX']);
save('GFX_Data_inGaits', ...
    'GFX_headY', 'GFX_TargPosData','GFX_RespPosData',...
    'subjIDs', 'pidx1', 'pidx2', 'gaittypes');%, '-append');

